// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/performance-dialog-chart.js

window.AmChartMakers = window.AmChartMakers || {};

// PerformanceChartDialog 전용 차트 Maker
window.AmChartMakers.PerformanceDialogChart = {
    create: function (root, data, config) {
        const chart = root.container.children.push(am5xy.XYChart.new(root, {
            panX: true, panY: true, wheelX: "panX", wheelY: "zoomX",
            layout: root.verticalLayout
        }));

        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
        const gridColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

        const cursor = chart.set("cursor", am5xy.XYCursor.new(root, { behavior: "zoomXY" }));
        const cursorColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
        cursor.lineX.set("stroke", cursorColor);
        cursor.lineY.set("stroke", cursorColor);

        // X축 설정
        const xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: config.xTimeUnit || "minute", count: 1 },
            renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 80 }),
            inputDateFormat: "yyyy-MM-ddTHH:mm:ss"
        }));

        if (config.xAxisDateFormat) {
            xAxis.get("dateFormats")["minute"] = config.xAxisDateFormat;
            xAxis.get("dateFormats")["hour"] = config.xAxisDateFormat;
            xAxis.get("dateFormats")["day"] = "MM-dd";
            xAxis.get("dateFormats")["month"] = "yyyy-MM";
        }

        xAxis.get("renderer").labels.template.setAll({ fill: textColor, rotation: -45, centerY: am5.p50, centerX: am5.p100, paddingRight: 10 });
        xAxis.get("renderer").grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });

        // Y축 설정
        const yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {})
        }));
        yAxis.get("renderer").labels.template.set("fill", textColor);
        yAxis.get("renderer").grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });
        yAxis.set("min", 0);

        // 시리즈(그래프) 설정
        config.series.forEach(seriesConfig => {
            const tooltip = am5.Tooltip.new(root, {
                labelText: seriesConfig.tooltipText,
                getFillFromSprite: true,
                getLabelFillFromSprite: true
            });

            const series = chart.series.push(am5xy.LineSeries.new(root, {
                name: seriesConfig.name,
                xAxis: xAxis,
                yAxis: yAxis,
                valueYField: seriesConfig.valueField,
                valueXField: config.xField,
                stroke: am5.color(seriesConfig.color),
                fill: am5.color(seriesConfig.color),
                tooltip: tooltip
            }));
            series.strokes.template.setAll({ strokeWidth: 2 });

            if (seriesConfig.bulletRadius > 0) {
                series.bullets.push(() => am5.Bullet.new(root, {
                    sprite: am5.Circle.new(root, {
                        radius: 4, // ⭐⭐⭐ 이 값을 3에서 4로 변경했습니다. ⭐⭐⭐
                        fill: series.get("stroke")
                    })
                }));
            }
            series.data.processor = am5.DataProcessor.new(root, { dateFields: [config.xField], dateFormat: "yyyy-MM-ddTHH:mm:ss" });
            series.data.setAll(data);
        });

        // 범례(Legend) 설정
        const legend = chart.children.push(am5.Legend.new(root, { centerX: am5.p50, x: am5.p50 }));
        legend.labels.template.set("fill", textColor);
        legend.data.setAll(chart.series.values);
    }
};
