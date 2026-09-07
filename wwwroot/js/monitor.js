'use strict';

let responseChart = null;

function initMonitor(statsUrl, chartUrl) {
    const btnLoad = document.getElementById('btnLoad');
    const sourceSelect = document.getElementById('sourceSelect');

    btnLoad.addEventListener('click', function () {
        loadData(statsUrl, chartUrl);
    });

    if (sourceSelect && sourceSelect.options.length > 0) {
        loadData(statsUrl, chartUrl);
    }
}

function loadData(statsUrl, chartUrl) {
    const sourceSelect = document.getElementById('sourceSelect');
    const dateFrom = document.getElementById('dateFrom');
    const dateTo = document.getElementById('dateTo');

    const id = sourceSelect ? sourceSelect.value : null;
    const from = dateFrom ? dateFrom.value : null;
    const to = dateTo ? dateTo.value : null;

    if (!id || !from || !to) return;

    const fromIso = new Date(from).toISOString();
    const toIso = new Date(to).toISOString();

    loadStats(statsUrl, id, fromIso, toIso);
    loadChart(chartUrl, id, fromIso, toIso);
}

function loadStats(statsUrl, id, from, to) {
    fetch(`${statsUrl}?id=${id}&from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`)
        .then(r => r.json())
        .then(result => {
            if (result.success) {
                renderStats(result.data);
            } else {
                clearStats(result.message);
            }
        })
        .catch(() => clearStats('Fehler beim Laden der Statistiken.'));
}

function loadChart(chartUrl, id, from, to) {
    fetch(`${chartUrl}?id=${id}&from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`)
        .then(r => r.json())
        .then(result => {
            if (result.success && result.data.length > 0) {
                renderChart(result.data);
                document.getElementById('chart-panel').style.display = 'block';
                document.getElementById('no-chart-panel').style.display = 'none';
            } else {
                if (responseChart) { responseChart.destroy(); responseChart = null; }
                document.getElementById('chart-panel').style.display = 'none';
                document.getElementById('no-chart-panel').style.display = 'block';
            }
        })
        .catch(() => {
            document.getElementById('chart-panel').style.display = 'none';
            document.getElementById('no-chart-panel').style.display = 'block';
        });
}

function renderStats(stats) {
    document.getElementById('stat-min').textContent = stats.MinResponseTime + ' ms';
    document.getElementById('stat-max').textContent = stats.MaxResponseTime + ' ms';
    document.getElementById('stat-avg').textContent = Math.round(stats.AvgResponseTime) + ' ms';
    document.getElementById('stat-count').textContent = stats.TotalCount;

    const uptimeEl = document.getElementById('stat-uptime');
    uptimeEl.textContent = stats.UptimePercent + ' %';
    uptimeEl.className = 'fw-bold fs-5 ' + (stats.UptimePercent >= 99 ? 'text-success' : stats.UptimePercent >= 90 ? 'text-warning' : 'text-danger');

    document.getElementById('stats-panel').style.display = 'flex';
    document.getElementById('no-stats-panel').style.display = 'none';
}

function clearStats(message) {
    document.getElementById('stats-panel').style.display = 'none';
    const noStats = document.getElementById('no-stats-panel');
    noStats.textContent = message || 'Keine Daten für den gewählten Zeitraum vorhanden.';
    noStats.style.display = 'block';
}

function renderChart(points) {
    const labels = points.map(function (p) {
        const d = new Date(p.Timestamp);
        return d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })
            + ' ' + d.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
    });

    const values = points.map(function (p) { return p.AvgResponseTime; });
    const bgColors = points.map(function (p) {
        return p.IsOnline ? 'rgba(91,153,234,0.75)' : 'rgba(220,53,69,0.75)';
    });
    const borderColors = points.map(function (p) {
        return p.IsOnline ? 'rgba(91,153,234,1)' : 'rgba(220,53,69,1)';
    });

    const ctx = document.getElementById('responseChart').getContext('2d');
    if (responseChart) {
        responseChart.destroy();
    }

    responseChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Ø Response-Zeit (ms)',
                data: values,
                backgroundColor: bgColors,
                borderColor: borderColors,
                borderWidth: 1,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: true, position: 'top' },
                tooltip: {
                    callbacks: {
                        afterLabel: function (ctx) {
                            return 'Status: ' + (points[ctx.dataIndex].IsOnline ? 'Online' : 'Offline');
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: { display: true, text: 'Response-Zeit (ms)' }
                },
                x: {
                    ticks: { maxRotation: 45, minRotation: 45 }
                }
            },
            interaction: {
                intersect: false,
                mode: 'index',
            },
        }
    });
}
