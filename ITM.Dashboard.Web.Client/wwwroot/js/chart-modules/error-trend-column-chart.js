// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/error-trend-column-chart.js

window.AmChartMakers = window.AmChartMakers || {};

// Error Analytics Trend 용 막대 차트 Maker
window.AmChartMakers.ErrorTrendColumnChart = {
    create: function (root, data, config) {
        const chart = root.container.children.push(am5xy.XYChart.new(root, {
            panX: false,
            panY: false,
            wheelX: "none",
            wheelY: "none",
            layout: root.verticalLayout
        }));

        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

        const xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: config.xTimeUnit, count: 1 },
            renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 60 }),
            dateFormats: { "day": config.xAxisDateFormat }
        }));
        xAxis.get("renderer").labels.template.setAll({ fill: textColor, rotation: -45, centerY: am5.p50, centerX: am5.p100, paddingRight: 10 });

        const yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {}),
            min: 0
        }));
        yAxis.get("renderer").labels.template.set("fill", textColor);

        config.series.forEach(seriesConfig => {
            const series = chart.series.push(am5xy.ColumnSeries.new(root, {
                name: seriesConfig.name,
                xAxis: xAxis,
                yAxis: yAxis,
                valueYField: seriesConfig.valueField,
                valueXField: config.xField,
                fill: am5.color(seriesConfig.color),
                // 막대 자체의 툴팁은 그대로 유지합니다.
                tooltip: am5.Tooltip.new(root, {
                    labelText: seriesConfig.tooltipText,
                    pointerOrientation: "down"
                })
            }));
            series.columns.template.setAll({ cornerRadiusTL: 5, cornerRadiusTR: 5, cursorOverStyle: "pointer" });
            series.bullets.push(() => am5.Bullet.new(root, {
                locationY: 1,
                sprite: am5.Label.new(root, { text: "{valueY}건", fill: textColor, centerY: am5.p100, centerX: am5.p50, populateText: true, paddingBottom: 5 })
            }));
            series.data.processor = am5.DataProcessor.new(root, { dateFields: [config.xField], dateFormat: "yyyy-MM-ddTHH:mm:ss" });
            series.data.setAll(data);
        });

        // ▼▼▼ [핵심 수정] 문제를 일으켰던 chart.set("cursor", ...) 라인 전체를 완전히 삭제합니다. ▼▼▼
        // 이 코드를 제거함으로써 더 이상 커서 관련 툴팁이 나타나지 않습니다.
    }
};
