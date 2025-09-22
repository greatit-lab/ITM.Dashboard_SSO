// ITM.Dashboard.Web.Client/wwwroot/js/chart-modules/error-by-eqp-pie-chart.js

window.AmChartMakers = window.AmChartMakers || {};

// Error Analytics 장비별 현황 파이 차트 Maker
window.AmChartMakers.ErrorByEqpPieChart = {
    create: function (root, data, config) {
        const chart = root.container.children.push(am5percent.PieChart.new(root, {
            layout: root.verticalLayout,
            innerRadius: am5.percent(50)
        }));

        const series = chart.series.push(am5percent.PieSeries.new(root, {
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
    }
};
