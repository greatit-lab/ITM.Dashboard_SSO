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
                        const xyChart = root.container.children.push(am5xy.XYChart.new(root, {
                            panX: true, panY: false, wheelX: "panX", wheelY: "zoomX", layout: root.verticalLayout
                        }));
                        this.configureXYChart(root, xyChart, data, config);
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

    configureXYChart: (root, chart, data, config) => {
        const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
        const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
        const gridColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
    
        let cursor = chart.set("cursor", am5xy.XYCursor.new(root, {
            behavior: "zoomX"
        }));
        cursor.lineY.set("visible", false);
    
        let xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: config.xTimeUnit || "day", count: 1 },
            renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 80 }),
            inputDateFormat: "yyyy-MM-ddTHH:mm-ss",
        }));
    
        // C#에서 넘어온 baseInterval을 우선 적용 (5분 간격 데이터용)
        if (config.baseInterval) {
            xAxis.set("baseInterval", config.baseInterval);
        }
        // C#에서 넘어온 gridIntervals를 우선 적용 (60분 간격 레이블용)
        if (config.gridIntervals) {
            xAxis.set("gridIntervals", config.gridIntervals);
        }
    
        if (config.xAxisDateFormat) {
            xAxis.get("dateFormats")[config.xTimeUnit || "day"] = config.xAxisDateFormat;
            xAxis.get("dateFormats")["hour"] = config.xAxisDateFormat;
            xAxis.get("dateFormats")["minute"] = config.xAxisDateFormat;
        }
        
        let xRenderer = xAxis.get("renderer");
        xRenderer.labels.template.setAll({
            fill: textColor, rotation: -45, centerY: am5.p50,
            centerX: am5.p100, paddingRight: 10
        });
        xRenderer.grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });
    
        let yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {})
        }));
        let yRenderer = yAxis.get("renderer");
        yRenderer.labels.template.set("fill", textColor);
        if (config.yAxisMin !== undefined) yAxis.set("min", config.yAxisMin);
        if (config.yAxisForceInteger) yAxis.set("maxPrecision", 0);
        yRenderer.grid.template.setAll({ stroke: gridColor, strokeOpacity: 0.15 });
    
        config.series.forEach(seriesConfig => {
            let series;
            if (seriesConfig.seriesType === 'line') {
                series = chart.series.push(am5xy.LineSeries.new(root, {
                    name: seriesConfig.name,
                    xAxis: xAxis,
                    yAxis: yAxis,
                    valueYField: seriesConfig.valueField,
                    valueXField: config.xField,
                    stroke: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                    fill: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                    tooltip: am5.Tooltip.new(root, {
                        getFillFromSprite: true,
                        getLabelFillFromSprite: true,
                        pointerOrientation: "horizontal",
                        labelText: "{valueX.formatDate('HH:mm')} {name}: {valueY.formatNumber('#.00')} %"
                    })
                }));
                
                series.strokes.template.setAll({ strokeWidth: 2 });
                series.bullets.push(function () {
                    return am5.Bullet.new(root, {
                        sprite: am5.Circle.new(root, {
                            radius: 4,
                            fill: series.get("fill")
                        })
                    });
                });
            } else { // 기본값: ColumnSeries (Error Analytics용)
                series = chart.series.push(am5xy.ColumnSeries.new(root, {
                    name: seriesConfig.name,
                    xAxis: xAxis,
                    yAxis: yAxis,
                    valueYField: seriesConfig.valueField,
                    valueXField: config.xField,
                    tooltip: am5.Tooltip.new(root, {
                        labelText: `{valueX.formatDate('${config.xAxisDateFormat}')}: {valueY}건`
                    })
                }));
    
                series.columns.template.setAll({ cornerRadiusTL: 5, cornerRadiusTR: 5, cursorOverStyle: "pointer" });
                
                series.bullets.push(function () {
                    return am5.Bullet.new(root, {
                        locationY: 1,
                        sprite: am5.Label.new(root, {
                            text: "{valueY}건",
                            fill: textColor,
                            centerY: am5.p100, centerX: am5.p50,
                            populateText: true, paddingBottom: 5
                        })
                    });
                });
            }
            
            series.data.processor = am5.DataProcessor.new(root, {
                dateFields: [config.xField],
                dateFormat: "yyyy-MM-ddTHH:mm:ss"
            });
            series.data.setAll(data);
            series.appear(1000);
        });
    
        let legend = chart.children.push(am5.Legend.new(root, {
            centerX: am5.p50, x: am5.p50
        }));
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

        // ▼▼▼ [수정] 차트 조각(slice)에 마우스를 올렸을 때 나오는 툴팁의 텍스트를 설정합니다. ▼▼▼
        series.slices.template.set("tooltipText", "{category}: {value}건");

        // ▼▼▼ [수정] 차트에 항상 표시되는 라벨의 텍스트를 설정합니다. ▼▼▼
        series.labels.template.setAll({
            text: "{category}: {value}건", // 라벨에는 EQPID(category)만 표시하여 간결하게 만듭니다.
            fill: root.interfaceColors.get("text"),
            textType: "circular",
            centerX: 0,
            centerY: 0,
            fontSize: 10,
        });

        // 라벨과 차트 조각을 연결하는 선(tick)은 숨깁니다.
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
