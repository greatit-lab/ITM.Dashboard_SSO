// ITM.Dashboard.Web.Client/wwwroot/js/amcharts-interop.js

configureXYChart: (root, chart, data, config) => {
    const isDarkMode = document.body.querySelector('.dark-theme-main-content') !== null;
    const textColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);
    const gridColor = isDarkMode ? am5.color(0xffffff) : am5.color(0x000000);

    let cursor = chart.set("cursor", am5xy.XYCursor.new(root, {
        behavior: "zoomX"
    }));
    cursor.lineY.set("visible", false);

    let xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
        baseInterval: { timeUnit: "minute", count: 5 },
        renderer: am5xy.AxisRendererX.new(root, { minGridDistance: 80 }),
        inputDateFormat: "yyyy-MM-ddTHH:mm:ss",
    }));

    xAxis.set("gridIntervals", [
        { timeUnit: "minute", count: 60 }
    ]);

    xAxis.get("dateFormats")["minute"] = "HH:mm";
    xAxis.get("dateFormats")["hour"] = "HH:mm";
    xAxis.get("dateFormats")["day"] = "yy-MM-dd";
    
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
                // ▼▼▼ [핵심 수정] 시리즈 자체에 fill과 stroke을 설정합니다. ▼▼▼
                stroke: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                fill: seriesConfig.color ? am5.color(seriesConfig.color) : undefined,
                tooltip: am5.Tooltip.new(root, {
                    getFillFromSprite: true, // 툴팁 배경을 시리즈의 fill 색상으로 설정
                    getLabelFillFromSprite: true, // 툴팁 글자색을 자동으로 조절
                    pointerOrientation: "horizontal",
                    labelText: "{valueX.formatDate('HH:mm')} {name}: {valueY.formatNumber('#.00')} %"
                })
            }));
            
            series.strokes.template.setAll({ strokeWidth: 2 });
            
            series.bullets.push(function () {
                return am5.Bullet.new(root, {
                    sprite: am5.Circle.new(root, {
                        radius: 4,
                        // 점의 색상은 이제 자동으로 시리즈의 fill 색상을 따라갑니다.
                        fill: series.get("fill")
                    })
                });
            });
        } else {
            // (ColumnSeries 코드는 변경 없음)
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
