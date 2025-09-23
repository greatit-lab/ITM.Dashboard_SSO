// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/pre-align-analytics-chart.js

window.AmChartMakers = window.AmChartMakers || {};

window.AmChartMakers.PreAlignAnalyticsChart = {
  create: function (root, data, config) {
    // 차트 생성
    const chart = root.container.children.push(
      am5xy.XYChart.new(root, {
        panX: true,
        panY: true,
        wheelX: "panX",
        wheelY: "zoomY",        // 휠로 Y축 줌
        pinchZoomY: true,       // 터치 핀치 Y축 줌
        layout: root.horizontalLayout // 좌/우에 커스텀 스크롤바 배치 용이
      })
    );

    const isDark = document.body.querySelector(".dark-theme-main-content") !== null;
    const textColor = isDark ? am5.color(0xffffff) : am5.color(0x000000);

    // X축
    const xAxis = chart.xAxes.push(
      am5xy.DateAxis.new(root, {
        baseInterval: { timeUnit: config.xTimeUnit, count: 1 },
        renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 120 }),
        inputDateFormat: "yyyy-MM-ddTHH:mm:ss"
      })
    );
    const fmt = "yy-MM-dd HH:mm";
    xAxis.get("dateFormats")["minute"] = fmt;
    xAxis.get("dateFormats")["hour"] = fmt;
    xAxis.get("dateFormats")["day"] = fmt;
    xAxis.get("periodChangeDateFormats")["day"] = fmt;
    xAxis.get("renderer").labels.template.setAll({
      fill: textColor,
      rotation: -45,
      centerY: am5.p50,
      centerX: am5.p100,
      paddingRight: 10
    });

    // Y축들 (Y1: 왼쪽, Y2: 오른쪽)
    config.yAxes.forEach(y => {
      const r = am5xy.AxisRendererY.new(root, { opposite: y.opposite || false });
      r.labels.template.set("fill", textColor);
      const axis = chart.yAxes.push(am5xy.ValueAxis.new(root, { renderer: r }));
      if (y.min !== undefined) axis.set("min", y.min);
      if (y.max !== undefined) axis.set("max", y.max);
    });

    // 시리즈
    config.series.forEach(s => {
      const tooltip = am5.Tooltip.new(root, {
        labelText: s.tooltipText,
        getFillFromSprite: true,
        getLabelFillFromSprite: true
      });

      const series = chart.series.push(
        am5xy.LineSeries.new(root, {
          name: s.name,
          xAxis,
          yAxis: chart.yAxes.getIndex(s.yAxisIndex),
          valueYField: s.valueField,
          valueXField: config.xField,
          stroke: am5.color(s.color),
          fill: am5.color(s.color),
          tooltip
        })
      );

      if (s.bulletRadius > 0) {
        series.bullets.push(() =>
          am5.Bullet.new(root, {
            sprite: am5.Circle.new(root, {
              radius: s.bulletRadius,
              fill: series.get("stroke")
            })
          })
        );
      }

      series.data.processor = am5.DataProcessor.new(root, {
        dateFields: [config.xField],
        dateFormat: "yyyy-MM-ddTHH:mm:ss"
      });
      series.data.setAll(data);
    });

    // 범례/커서
    const legend = chart.children.push(
      am5.Legend.new(root, { centerY: am5.p50, y: am5.p50, layout: root.verticalLayout })
    );
    legend.labels.template.set("fill", textColor);
    legend.data.setAll(chart.series.values);
    chart.set("cursor", am5xy.XYCursor.new(root, { behavior: "zoomXY" }));

    // ========= 핵심: 각 축 전용 커스텀 세로 스크롤바 연결 =========
    function clamp01(v) {
      return Math.max(0, Math.min(1, v));
    }

    function wireAxisScrollbar(axis, sideContainer, widthPx = 48) {
      const sb = am5.Scrollbar.new(root, {
        orientation: "vertical",
        width: widthPx
      });

      // 배치
      sideContainer.children.push(sb);

      // 초기 동기화 (축 -> 스크롤바)
      const aStart = axis.get("start") ?? 0;
      const aEnd = axis.get("end") ?? 1;
      sb.setAll({ start: clamp01(aStart), end: clamp01(aEnd) });

      // 스크롤바 -> 축
      sb.events.on("rangechanged", () => {
        const s = clamp01(sb.get("start") ?? 0);
        const e = clamp01(sb.get("end") ?? 1);
        // start와 end가 동일해지는 걸 방지
        const EPS = 0.0001;
        const ss = Math.min(s, e - EPS);
        const ee = Math.max(e, s + EPS);
        axis.zoom(ss, ee); // 해당 축만 줌
      });

      // 축 -> 스크롤바 (마우스휠/드래그 등 다른 줌 동작과 동기화)
      axis.on("start", (val) => sb.set("start", clamp01(val ?? 0)));
      axis.on("end", (val) => sb.set("end", clamp01(val ?? 1)));

      return sb;
    }

    // Y1(왼쪽) 전용 스크롤바
    const y1 = chart.yAxes.getIndex(0);
    wireAxisScrollbar(y1, chart.leftAxesContainer, 50);

    // Y2(오른쪽) 전용 스크롤바
    const y2 = chart.yAxes.getIndex(1);
    wireAxisScrollbar(y2, chart.rightAxesContainer, 50);

    // ✅ 주의: chart.set("scrollbarY", ...) 또는 XYChartScrollbar는 사용하지 않습니다.
    // (차트 레벨 스크롤바는 모든 Y축에 동시에 적용됨)

    // ============================================================
  }
};
