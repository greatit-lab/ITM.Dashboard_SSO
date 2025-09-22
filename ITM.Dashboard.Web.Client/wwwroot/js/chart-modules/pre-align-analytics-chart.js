// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/pre-align-analytics-chart.js

window.AmChartMakers = window.AmChartMakers || {};

window.AmChartMakers.PreAlignAnalyticsChart = {
    create: function (root, data, config) {
        const chart = root.container.children.push(am5xy.XYChart.new(root, { panX: true, panY: true, wheelX: "panX", wheelY: "zoomX", layout: root.verticalLayout }));
        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

        const xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: config.xTimeUnit, count: 1 },
            renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 120 }),
            inputDateFormat: "yyyy-MM-ddTHH:mm:ss"
        }));

        // ▼▼▼ [최종 수정] 일반 라벨과 날짜 변경 시점의 라벨 형식을 모두 통일합니다. ▼▼▼
        const consistentFormat = "yy-MM-dd HH:mm";
        xAxis.get("dateFormats")["minute"] = consistentFormat;
        xAxis.get("dateFormats")["hour"] = consistentFormat;
        xAxis.get("dateFormats")["day"] = consistentFormat;
        xAxis.get("dateFormats")["week"] = consistentFormat;
        xAxis.get("dateFormats")["month"] = "yyyy-MM";

        // 날짜가 바뀔 때 표시되는 라벨 형식도 위와 동일하게 설정
        xAxis.get("periodChangeDateFormats")["minute"] = consistentFormat;
        xAxis.get("periodChangeDateFormats")["hour"] = consistentFormat;
        xAxis.get("periodChangeDateFormats")["day"] = consistentFormat;
        xAxis.get("periodChangeDateFormats")["week"] = consistentFormat;
        xAxis.get("periodChangeDateFormats")["month"] = "yyyy-MM";
        // ▲▲▲ [최종 수정] ▲▲▲

        xAxis.get("renderer").labels.template.setAll({ fill: textColor, rotation: -45, centerY: am5.p50, centerX: am5.p100, paddingRight: 10 });

        // Y축 설정
        config.yAxes.forEach(yConfig => {
            const yRenderer = am5xy.AxisRendererY.new(root, { opposite: yConfig.opposite || false });
            yRenderer.labels.template.set("fill", textColor);
            const yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, { renderer: yRenderer }));
            if (yConfig.min !== undefined) yAxis.set("min", yConfig.min);
            if (yConfig.max !== undefined) yAxis.set("max", yConfig.max);
        });

        // 시리즈 설정
        config.series.forEach(seriesConfig => {
            const tooltip = am5.Tooltip.new(root, {
                labelText: seriesConfig.tooltipText,
                getFillFromSprite: true,
                getLabelFillFromSprite: true
            });

            const series = chart.series.push(am5xy.LineSeries.new(root, {
                name: seriesConfig.name,
                xAxis: xAxis,
                yAxis: chart.yAxes.getIndex(seriesConfig.yAxisIndex),
                valueYField: seriesConfig.valueField,
                valueXField: config.xField,
                stroke: am5.color(seriesConfig.color),
                fill: am5.color(seriesConfig.color),
                tooltip: tooltip
            }));

            if (seriesConfig.bulletRadius > 0) {
                series.bullets.push(() => am5.Bullet.new(root, {
                    sprite: am5.Circle.new(root, {
                        radius: seriesConfig.bulletRadius,
                        fill: series.get("stroke")
                    })
                }));
            }

            series.data.processor = am5.DataProcessor.new(root, { dateFields: [config.xField], dateFormat: "yyyy-MM-ddTHH:mm:ss" });
            series.data.setAll(data);
        });

        const legend = chart.children.push(am5.Legend.new(root, { centerX: am5.p50, x: am5.p50 }));
        legend.labels.template.set("fill", textColor);
        legend.data.setAll(chart.series.values);
        chart.set("cursor", am5xy.XYCursor.new(root, { behavior: "zoomXY" }));
    }
};
