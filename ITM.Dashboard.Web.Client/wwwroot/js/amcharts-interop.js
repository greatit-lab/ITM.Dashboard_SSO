// ITM.Dashboard.Web.Client/wwwroot/js/amcharts-interop.js

window.amChartInstances = window.amChartInstances || {};

function ensureAmChartsReady(callback, chartId, retries = 10, delay = 100) {
    if (window.am5 && window.am5themes_Dark && window.am5themes_Animated && window.AmChartMakers) {
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

                if (!data || data.length === 0) return;

                const chartMaker = window.AmChartMakers[chartType];
                if (chartMaker) {
                    chartMaker.create(root, data, config);
                } else {
                    console.error(`[AmChartsInterop] Unknown chart type: ${chartType}`);
                }

            } catch (e) {
                console.error(`[AmChartsInterop] Error during chart operation for ${chartId}:`, e);
            }
        }, chartId);
    },

    // ▼▼▼ [추가] Y축 범위를 업데이트하는 새로운 함수 ▼▼▼
    updateYAxisRange: function (chartId, axisIndex, min, max) {
        const root = window.amChartInstances[chartId];
        if (!root) return;

        const chart = root.container.children.getIndex(0);

        if (chart && chart.yAxes) {
            const axis = chart.yAxes.getIndex(axisIndex);
            if (axis) {
                axis.set("min", min);
                axis.set("max", max);
            } else {
                console.error(`[AmChartsInterop] Y-axis at index ${axisIndex} not found for chart ${chartId}.`);
            }
        } else {
            console.error(`[AmChartsInterop] Chart object not found for chart ${chartId}.`);
        }
    },

    disposeChart: function (chartId) {
        if (window.amChartInstances[chartId]) {
            window.amChartInstances[chartId].dispose();
            delete window.amChartInstances[chartId];
        }
    }
};
