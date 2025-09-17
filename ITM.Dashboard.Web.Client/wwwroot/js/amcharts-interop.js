// ITM.Dashboard.Web.Client/wwwroot/js/amcharts-interop.js

window.amChartInstances = window.amChartInstances || {};

function ensureAmChartsReady(callback, chartId, retries = 10, delay = 100) {
    if (window.am5 && window.am5themes_Dark && window.am5themes_Animated) {
        callback();
    } else if (retries > 0) {
        setTimeout(() => ensureAmChartsReady(callback, chartId, retries - 1, delay), delay);
    } else {
        console.error(`[AmChartsInterop] Failed to load amCharts libraries for ${chartId}.`);
    }
}

window.AmChartsInterop = {
    createOrUpdateChart: function (chartId, chartType, data, config, isDarkMode) {
        ensureAmChartsReady(() => {
            try {
                let root = window.amChartInstances[chartId];
                if (!root) {
                    root = am5.Root.new(chartId);
                    window.amChartInstances[chartId] = root;
                }

                const theme = isDarkMode ? am5themes_Dark.new(root) : am5themes_Animated.new(root);
                root.setThemes([theme]);

                root.container.children.clear();
                root._logo?.dispose();

                if (!data || data.length === 0) {
                    return;
                }

                switch (chartType) {
                    case 'XYChart':
                    case 'LineChart':
                    case 'ColumnChart':
                        const xyChart = root.container.children.push(am5xy.XYChart.new(root, {
                            panX: true, panY: true, wheelX: "panX", wheelY: "zoomX",
                            layout: root.verticalLayout
                        }));
                        this.configureXYChart(root, xyChart, data, config, chartType);
                        break;

                    case 'PieChart':
                        const pieChart = root.container.children.push(am5percent.PieChart.new(root, {
                            layout: root.verticalLayout, innerRadius: am5.percent(50)
                        }));
                        this.configurePieChart(root, pieChart, data, config);
                        break;
                }
            } catch (e) {
                console.error(`[AmChartsInterop] Error during chart operation for ${chartId}:`, e);
            }
        }, chartId);
    },

    configureXYChart: (root, chart, data, config, chartType) => {
        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
        const gridColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

        let cursor = chart.set("cursor", am5xy.XYCursor.new(root, { behavior: "zoomXY" }));
        cursor.lineY.set("visible", false);

        let xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: config.xTimeUnit || "day", count: 1 },
            renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 80 }),
            inputDateFormat: "yyyy-MM-ddTHH:mm:ss"
        }));

        if (config.xAxisDateFormat) {
            xAxis.get("dateFormats")[config.xTimeUnit || "day"] = config.xAxisDateFormat;
            xAxis.get("dateFormats")["hour"] = "yy-MM-dd HH:mm";
            xAxis.get("dateFormats")["minute"] = "yy-MM-dd HH:mm";
        }

        let xRenderer = xAxis.get("renderer");
        xRenderer.labels.template.setAll({
            fill: textColor, rotation: -45, centerY: am5.p50,
            centerX: am5.p100, paddingRight: 10
        });
        xRenderer.grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });

        if (config.yAxes && Array.isArray(config.yAxes)) {
            config.yAxes.forEach(yAxisConfig => {
                const renderer = am5xy.AxisRendererY.new(root, { opposite: yAxisConfig.opposite || false });
                renderer.labels.template.set("fill", textColor);
                renderer.grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });
                const yAxis = am5xy.ValueAxis.new(root, { renderer: renderer });
                if (yAxisConfig.min !== undefined) yAxis.set("min", yAxisConfig.min);
                if (yAxisConfig.max !== undefined) yAxis.set("max", yAxisConfig.max);
                chart.yAxes.push(yAxis);
            });
        } else {
            let yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
                renderer: am5xy.AxisRendererY.new(root, {})
            }));
            let yRenderer = yAxis.get("renderer");
            yRenderer.labels.template.set("fill", textColor);
            if (config.yAxisMin !== undefined) yAxis.set("min", config.yAxisMin);
            if (config.yAxisForceInteger) yAxis.set("maxPrecision", 0);
            yRenderer.grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });
        }

        config.series.forEach(seriesConfig => {
            const yAxisIndex = seriesConfig.yAxisIndex || 0;
            const yAxis = chart.yAxes.getIndex(yAxisIndex);
            let series;

            const tooltip = am5.Tooltip.new(root, {
                getFillFromSprite: true,
                getLabelFillFromSprite: true,
                pointerOrientation: "horizontal",
                labelText: seriesConfig.tooltipText || "{valueX.formatDate('HH:mm')} {name}: {valueY}"
            });

            if (seriesConfig.seriesType === 'column') {
                series = chart.series.push(am5xy.ColumnSeries.new(root, {
                    name: seriesConfig.name,
                    xAxis: xAxis,
                    yAxis: yAxis,
                    valueYField: seriesConfig.valueField,
                    valueXField: config.xField,
                    fill: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                    tooltip: tooltip
                }));
                series.columns.template.setAll({ cornerRadiusTL: 5, cornerRadiusTR: 5, cursorOverStyle: "pointer" });

                // ▼▼▼ [추가] 막대그래프 위에 데이터 값 라벨을 추가합니다. ▼▼▼
                series.bullets.push(() => am5.Bullet.new(root, {
                    locationY: 1, // 막대 상단에 위치
                    sprite: am5.Label.new(root, {
                        text: "{valueY}건", // '건' 단위 추가
                        fill: textColor,
                        centerY: am5.p100,
                        centerX: am5.p50,
                        populateText: true,
                        paddingBottom: 5 // 막대와의 간격
                    })
                }));
                // ▲▲▲ [추가] 여기까지가 추가된 부분입니다. ▲▲▲

            }
            else {
                series = chart.series.push(am5xy.LineSeries.new(root, {
                    name: seriesConfig.name,
                    xAxis: xAxis,
                    yAxis: yAxis,
                    valueYField: seriesConfig.valueField,
                    valueXField: config.xField,
                    stroke: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                    fill: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                    tooltip: tooltip
                }));

                series.strokes.template.setAll({ strokeWidth: 2 });
                const bulletRadius = seriesConfig.bulletRadius === undefined ? 4 : seriesConfig.bulletRadius;
                if (bulletRadius > 0) {
                    series.bullets.push(() => am5.Bullet.new(root, {
                        sprite: am5.Circle.new(root, { radius: bulletRadius, fill: series.get("fill") })
                    }));
                }
            }

            series.data.processor = am5.DataProcessor.new(root, {
                dateFields: [config.xField],
                dateFormat: "yyyy-MM-ddTHH:mm:ss"
            });
            series.data.setAll(data);
            series.appear(1000);
        });

        let legend = chart.children.push(am5.Legend.new(root, { centerX: am5.p50, x: am5.p50 }));
        legend.labels.template.set("fill", textColor);
        legend.data.setAll(chart.series.values);

        chart.appear(1000, 100);
    },

    configurePieChart: (root, chart, data, config) => {
        let series = chart.series.push(am5percent.PieSeries.new(root, {
            valueField: config.valueField,
            categoryField: config.categoryField,
            alignLabels: false
        }));

        series.slices.template.set("tooltipText", "{category}: {value}건");
        series.labels.template.setAll({
            text: "{category}: {value}건",
            fill: root.interfaceColors.get("text"),
            textType: "circular",
            centerX: 0, centerY: 0, fontSize: 10,
        });

        series.ticks.template.set("forceHidden", true);
        series.data.setAll(data);
        series.appear(1000, 100);
    },

    disposeChart: function (chartId) {
        if (window.amChartInstances[chartId]) {
            window.amChartInstances[chartId].dispose();
            delete window.amChartInstances[chartId];
        }
    }
};
