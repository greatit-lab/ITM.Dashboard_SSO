// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/lot-uniformity-chart.js
window.AmChartMakers = window.AmChartMakers || {};

window.AmChartMakers.LotUniformityChart = {
    create: function (root, data, config) {
        const chart = root.container.children.push(am5xy.XYChart.new(root, {
            panX: true,
            panY: true,
            wheelX: "panX",
            wheelY: "zoomX",
            pinchZoomX: true,
            layout: root.horizontalLayout
        }));

        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

        const cursor = chart.set("cursor", am5xy.XYCursor.new(root, {
            behavior: "zoomXY",
            // ▼▼▼ [수정 1] snapToSeries 속성을 추가할 것이므로, Y축 라인은 숨깁니다. ▼▼▼
            lineY: { visible: false }
        }));

        // X축 (Point #)
        const xAxis = chart.xAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererX.new(root, {}),
            tooltip: am5.Tooltip.new(root, {})
        }));
        xAxis.get("renderer").labels.template.setAll({ fill: textColor });
        xAxis.children.push(am5.Label.new(root, { text: config.xTitle, x: am5.p50, centerX: am5.p50, fill: textColor }));

        // Y축 (Metric Value)
        const yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {}),
            tooltip: am5.Tooltip.new(root, {})
        }));
        yAxis.get("renderer").labels.template.setAll({ fill: textColor });
        yAxis.children.moveValue(am5.Label.new(root, { text: config.yTitle, rotation: -90, y: am5.p50, centerX: am5.p50, fill: textColor }), 0);

        const legend = chart.children.push(am5.Legend.new(root, {
            centerY: am5.p50,
            y: am5.p50,
            layout: root.verticalLayout
        }));
        legend.labels.template.setAll({ fill: textColor });

        // WaferId별로 데이터를 그룹화하여 시리즈 생성
        const waferIds = [...new Set(data.map(item => item.waferId))];

        // ▼▼▼ [수정 2] 생성된 모든 시리즈를 담을 배열을 선언합니다. ▼▼▼
        const allSeries = [];

        waferIds.forEach(waferId => {
            const series = chart.series.push(am5xy.LineSeries.new(root, {
                name: "Wafer " + waferId,
                xAxis: xAxis,
                yAxis: yAxis,
                valueYField: "value",
                valueXField: "point",
                tooltip: am5.Tooltip.new(root, {
                    pointerOrientation: "horizontal",
                    labelText: "[bold]{name}:[/] {valueY.formatNumber('#.00')}"
                })
            }));

            series.strokes.template.setAll({
                strokeWidth: 2
            });

            const waferData = data.filter(d => d.waferId === waferId);
            series.data.setAll(waferData);
            legend.data.push(series);

            // ▼▼▼ [수정 3] 생성된 시리즈를 배열에 추가합니다. ▼▼▼
            allSeries.push(series);
        });

        // ▼▼▼ [수정 4] 커서가 모든 시리즈 중 가장 가까운 시리즈에 달라붙도록 설정합니다. ▼▼▼
        cursor.set("snapToSeries", allSeries);

        // 범례 항목에 마우스를 올리면 해당 시리즈를 강조하는 이벤트
        legend.itemContainers.template.events.on("pointerover", function (e) {
            let series = e.target.dataItem.dataContext;
            chart.series.each(function (chartSeries) {
                if (chartSeries != series) {
                    chartSeries.strokes.template.setAll({
                        strokeOpacity: 0.3,
                        stroke: am5.color(0x888888)
                    });
                } else {
                    chartSeries.strokes.template.set("strokeWidth", 3);
                }
            });
        });

        legend.itemContainers.template.events.on("pointerout", function (e) {
            chart.series.each(function (chartSeries) {
                chartSeries.strokes.template.setAll({
                    strokeOpacity: 1,
                    strokeWidth: 2,
                    stroke: chartSeries.get("stroke")
                });
            });
        });

        // 차트 상단에 스크롤바/줌 기능 추가
        const scrollbar = chart.set("scrollbarX", am5xy.XYChartScrollbar.new(root, {
            orientation: "horizontal",
            height: 50
        }));

        const sbxAxis = scrollbar.chart.xAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererX.new(root, {})
        }));

        const sbyAxis = scrollbar.chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {})
        }));

        waferIds.forEach(waferId => {
            const sbSeries = scrollbar.chart.series.push(am5xy.LineSeries.new(root, {
                xAxis: sbxAxis,
                yAxis: sbyAxis,
                valueYField: "value",
                valueXField: "point"
            }));
            const waferData = data.filter(d => d.waferId === waferId);
            sbSeries.data.setAll(waferData);
        });

        chart.appear(1000, 100);
    }
};
