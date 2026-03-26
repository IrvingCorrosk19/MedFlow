(function (window, document, Chart) {
    'use strict';

    var P = {
        primary: 'rgba(37, 99, 235, 0.7)',
        primaryLine: 'rgb(37, 99, 235)',
        success: 'rgba(16, 185, 129, 0.65)',
        successLine: 'rgb(16, 185, 129)',
        danger: 'rgba(239, 68, 68, 0.55)',
        warning: 'rgba(245, 158, 11, 0.75)',
        info: 'rgba(14, 165, 233, 0.65)',
        purple: 'rgba(99, 102, 241, 0.65)',
        doughnut: ['#2563eb', '#10b981', '#f59e0b', '#ef4444', '#64748b', '#0ea5e9']
    };

    function barChart(canvasId, labels, data, label, color) {
        var el = document.getElementById(canvasId);
        if (!el || !labels || !labels.length) return;
        new Chart(el, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: color || P.primary,
                    borderColor: color || P.primaryLine,
                    borderWidth: 1,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(148, 163, 184, 0.2)' } },
                    x: { grid: { display: false } }
                }
            }
        });
    }

    function doughnutChart(canvasId, labels, data, colors) {
        var el = document.getElementById(canvasId);
        if (!el || !labels || !labels.length) return;
        new Chart(el, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors || P.doughnut,
                    borderWidth: 0,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '58%',
                plugins: {
                    legend: { position: 'bottom', labels: { usePointStyle: true, padding: 14, font: { size: 11 } } }
                }
            }
        });
    }

    function lineChart(canvasId, labels, data, label, color) {
        var el = document.getElementById(canvasId);
        if (!el || !labels || !labels.length) return;
        new Chart(el, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    fill: true,
                    backgroundColor: 'rgba(16, 185, 129, 0.12)',
                    borderColor: color || P.successLine,
                    tension: 0.35,
                    borderWidth: 2,
                    pointRadius: 3,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: true, labels: { font: { size: 11 } } } },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(148, 163, 184, 0.2)' } },
                    x: { grid: { display: false } }
                }
            }
        });
    }

    window.MedFlowExecutiveDashboard = {
        init: function (payload) {
            if (!payload || typeof Chart === 'undefined') return;
            barChart('chartAptDay', payload.appointmentsByDayLabels, payload.appointmentsByDayData, 'Citas', P.primary);
            doughnutChart('chartAptStatus', payload.statusLabels, payload.statusData);
            lineChart('chartRevenue', payload.revenueLabels, payload.revenueData, 'Ingresos (pagos)', P.successLine);
            barChart('chartNewPatients', payload.monthLabels, payload.newPatientsData, 'Pacientes', P.purple);
            doughnutChart('chartPayMethod', payload.payMethodLabels, payload.payMethodData, ['#10b981', '#6366f1', '#f59e0b', '#0ea5e9', '#64748b']);
            barChart('chartCancelTrend', payload.cancelLabels, payload.cancelData, 'Cancelaciones', P.danger);
            barChart('chartTopSpec', payload.topSpecLabels, payload.topSpecData, 'Citas', P.warning);
            barChart('chartTopDoc', payload.topDocLabels, payload.topDocData, 'Citas', P.info);
        }
    };
})(window, document, window.Chart);
