/**
 * SupplierSelectionDSS - Enterprise Decision Support Dashboard Scripts
 * Handles Live Clock, Batch Switcher, and Chart.js Initializations
 */

document.addEventListener("DOMContentLoaded", function () {
    // =========================================================
    // 1. LIVE CLOCK (ENGLISH LOCALE)
    // =========================================================
    const clockEl = document.getElementById("dashboard-live-clock");
    if (clockEl) {
        function updateClock() {
            const now = new Date();
            const days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
            const dayName = days[now.getDay()];
            const day = String(now.getDate()).padStart(2, "0");
            const month = String(now.getMonth() + 1).padStart(2, "0");
            const year = now.getFullYear();
            const hours = String(now.getHours()).padStart(2, "0");
            const minutes = String(now.getMinutes()).padStart(2, "0");
            const seconds = String(now.getSeconds()).padStart(2, "0");

            clockEl.replaceChildren();
            const icon = document.createElement("i");
            icon.className = "bi bi-clock";
            const textSpan = document.createElement("span");
            textSpan.textContent = ` ${dayName}, ${day}/${month}/${year} `;
            const timeStrong = document.createElement("strong");
            timeStrong.textContent = `${hours}:${minutes}:${seconds}`;
            textSpan.appendChild(timeStrong);
            clockEl.appendChild(icon);
            clockEl.appendChild(textSpan);
        }
        updateClock();
        setInterval(updateClock, 1000);
    }

    // =========================================================
    // 2. BATCH SELECTOR AUTO-REDIRECT
    // =========================================================
    const batchSelect = document.getElementById("batch-context-select");
    if (batchSelect) {
        batchSelect.addEventListener("change", function () {
            const selectedVal = this.value;
            if (selectedVal) {
                window.location.href = "/Dashboard?batchId=" + encodeURIComponent(selectedVal);
            }
        });
    }

    // =========================================================
    // 3. CHART.JS INITIALIZATION
    // =========================================================
    if (typeof Chart === "undefined") {
        console.warn("Chart.js is not loaded. Skipping chart rendering.");
        return;
    }

    // Global Chart defaults
    Chart.defaults.font.family = "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    Chart.defaults.color = "#64748b";

    // ---------------------------------------------------------
    // 3.1 DONUT: BATCH STATUS DISTRIBUTION
    // ---------------------------------------------------------
    const batchCanvas = document.getElementById("batchStatusDonutCanvas");
    if (batchCanvas && window.dashboardBatchData) {
        new Chart(batchCanvas, {
            type: "doughnut",
            data: {
                labels: window.dashboardBatchData.labels || [],
                datasets: [{
                    data: window.dashboardBatchData.values || [],
                    backgroundColor: window.dashboardBatchData.colors || ["#16a34a", "#2563eb", "#f59e0b", "#ef4444"],
                    borderWidth: 2,
                    borderColor: "#ffffff"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 1,
                cutout: "70%",
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const val = ctx.parsed || 0;
                                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                                const pct = total > 0 ? ((val / total) * 100).toFixed(0) : 0;
                                return ` ${ctx.label}: ${val} batches (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    // ---------------------------------------------------------
    // 3.2 DONUT: SUPPLIER STATUS DISTRIBUTION
    // ---------------------------------------------------------
    const supplierCanvas = document.getElementById("supplierStatusDonutCanvas");
    if (supplierCanvas && window.dashboardSupplierData) {
        new Chart(supplierCanvas, {
            type: "doughnut",
            data: {
                labels: window.dashboardSupplierData.labels || [],
                datasets: [{
                    data: window.dashboardSupplierData.values || [],
                    backgroundColor: window.dashboardSupplierData.colors || ["#16a34a", "#94a3b8"],
                    borderWidth: 2,
                    borderColor: "#ffffff"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 1,
                cutout: "70%",
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const val = ctx.parsed || 0;
                                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                                const pct = total > 0 ? ((val / total) * 100).toFixed(0) : 0;
                                return ` ${ctx.label}: ${val} suppliers (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    // ---------------------------------------------------------
    // 3.3 LINE: EVALUATION TREND OVER TIME
    // ---------------------------------------------------------
    const trendCanvas = document.getElementById("evaluationTrendCanvas");
    if (trendCanvas && window.dashboardTrendData) {
        const trendChart = new Chart(trendCanvas, {
            type: "line",
            data: {
                labels: window.dashboardTrendData.months || [],
                datasets: [
                    {
                        label: "Suppliers Evaluated",
                        data: window.dashboardTrendData.suppliers || [],
                        borderColor: "#2563eb",
                        backgroundColor: "rgba(37, 99, 235, 0.08)",
                        borderWidth: 2.5,
                        fill: true,
                        tension: 0.35,
                        pointBackgroundColor: "#2563eb",
                        pointBorderColor: "#ffffff",
                        pointBorderWidth: 2,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: "Evaluation Batches",
                        data: window.dashboardTrendData.batches || [],
                        borderColor: "#f59e0b",
                        backgroundColor: "transparent",
                        borderWidth: 2,
                        borderDash: [4, 4],
                        fill: false,
                        tension: 0.35,
                        pointBackgroundColor: "#f59e0b",
                        pointBorderColor: "#ffffff",
                        pointBorderWidth: 2,
                        pointRadius: 3.5,
                        pointHoverRadius: 5.5
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        mode: "index",
                        intersect: false,
                        backgroundColor: "rgba(15, 23, 42, 0.9)",
                        titleFont: { size: 12, weight: "bold" },
                        bodyFont: { size: 11.5 },
                        padding: 10,
                        cornerRadius: 8,
                        boxPadding: 4,
                        usePointStyle: true
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { size: 11.5, weight: 600 } }
                    },
                    y: {
                        beginAtZero: true,
                        grace: "15%",
                        grid: { color: "#f1f5f9" },
                        ticks: {
                            stepSize: 1,
                            precision: 0,
                            font: { size: 11 }
                        }
                    }
                }
            }
        });

        // Interactive HTML Legend for Trend Line Chart
        const legendPills = document.querySelectorAll("#trendLegendGroup .trend-legend-pill");
        legendPills.forEach(pill => {
            pill.addEventListener("click", function () {
                const datasetIndex = parseInt(this.getAttribute("data-dataset-index"), 10);
                if (isNaN(datasetIndex)) return;

                const isVisible = trendChart.isDatasetVisible(datasetIndex);
                trendChart.setDatasetVisibility(datasetIndex, !isVisible);
                trendChart.update();
                this.classList.toggle("is-hidden", isVisible);
            });
        });
    }

    // ---------------------------------------------------------
    // 3.4 HORIZONTAL BAR: TOPSIS RANKING
    // ---------------------------------------------------------
    const topsisBarCanvas = document.getElementById("topsisBarChartCanvas");
    if (topsisBarCanvas && window.dashboardTopsisBarData) {
        new Chart(topsisBarCanvas, {
            type: "bar",
            data: {
                labels: window.dashboardTopsisBarData.labels || [],
                datasets: [{
                    data: window.dashboardTopsisBarData.values || [],
                    backgroundColor: [
                        "#2563eb",
                        "#10b981",
                        "#f59e0b",
                        "#8b5cf6",
                        "#64748b"
                    ],
                    borderRadius: 6,
                    barPercentage: 0.65
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return ` CC Score: ${ctx.parsed.x.toFixed(4)}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        min: 0,
                        max: 1.0,
                        grid: { color: "#f1f5f9" },
                        ticks: { font: { size: 11 } }
                    },
                    y: {
                        grid: { display: false },
                        ticks: { font: { size: 12, weight: 600 } }
                    }
                }
            }
        });
    }

    // ---------------------------------------------------------
    // 3.5 RADAR: TOP 3 SUPPLIER COMPARISON (NORMALIZED 0-100)
    // ---------------------------------------------------------
    const radarCanvas = document.getElementById("radarComparisonCanvas");
    if (radarCanvas && window.dashboardRadarData && window.dashboardRadarData.datasets && window.dashboardRadarData.datasets.length > 0) {
        new Chart(radarCanvas, {
            type: "radar",
            data: {
                labels: window.dashboardRadarData.labels || [],
                datasets: window.dashboardRadarData.datasets.map(ds => {
                    const name = ds.SupplierName || ds.supplierName || "Supplier";
                    const color = ds.Color || ds.color || "#2563eb";
                    const scores = ds.Scores || ds.scores || [];
                    return {
                        label: name,
                        data: scores,
                        borderColor: color,
                        backgroundColor: color.startsWith("#") ? color + "22" : "rgba(37, 99, 235, 0.13)",
                        pointBackgroundColor: color,
                        pointBorderColor: "#ffffff",
                        pointHoverBackgroundColor: "#ffffff",
                        pointHoverBorderColor: color,
                        borderWidth: 2,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    };
                })
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: "top",
                        labels: {
                            boxWidth: 10,
                            boxHeight: 10,
                            usePointStyle: true,
                            font: { size: 11.5, weight: 600 }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            title: function (ctx) {
                                return ctx[0] ? ctx[0].label : "";
                            },
                            label: function (ctx) {
                                const dsIndex = ctx.datasetIndex;
                                const dataIndex = ctx.dataIndex;
                                const dsRaw = window.dashboardRadarData.datasets[dsIndex];
                                const supplierName = ctx.dataset.label || "Supplier";
                                const normVal = ctx.parsed.r;

                                let rawValText = "";
                                if (dsRaw) {
                                    if (dsRaw.FormattedScores && dsRaw.FormattedScores[dataIndex]) {
                                        rawValText = dsRaw.FormattedScores[dataIndex];
                                    } else if (dsRaw.formattedScores && dsRaw.formattedScores[dataIndex]) {
                                        rawValText = dsRaw.formattedScores[dataIndex];
                                    } else if (dsRaw.RawScores && dsRaw.RawScores[dataIndex] !== undefined) {
                                        rawValText = dsRaw.RawScores[dataIndex];
                                    }
                                }

                                if (rawValText) {
                                    return ` ${supplierName}: ${rawValText} (Normalized: ${normVal}/100)`;
                                }
                                return ` ${supplierName}: Normalized ${normVal}/100`;
                            }
                        }
                    }
                },
                scales: {
                    r: {
                        min: 0,
                        max: 100,
                        ticks: {
                            stepSize: 25,
                            display: true,
                            backdropColor: "transparent",
                            color: "#94a3b8",
                            font: { size: 9.5, weight: 500 },
                            callback: function (val) {
                                return val === 0 ? "" : val;
                            }
                        },
                        grid: { color: "rgba(226, 232, 240, 0.85)" },
                        angleLines: { color: "rgba(203, 213, 225, 0.7)" },
                        pointLabels: {
                            font: { size: 11, weight: 600 },
                            color: "#334155"
                        }
                    }
                }
            }
        });
    }
});
