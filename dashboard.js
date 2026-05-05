// assets/js/dashboard.js - Versión CORREGIDA y SIMPLIFICADA
// Sin plugins problemáticos

let puestosChart = null;
let brechaDeptoChart = null;
let donaChart = null;
let costoAnioChart = null;

// Pilar 1: Gráfico de barras - Costo proyectado por año
function initCostoAnioChart(labels, values) {
    const ctx = document.getElementById('costoAnioChart');
    if (!ctx) {
        console.error('No se encontró costoAnioChart');
        return;
    }
    
    if (costoAnioChart) {
        costoAnioChart.destroy();
        costoAnioChart = null;
    }
    
    if (!labels || labels.length === 0) {
        console.warn('No hay datos para costoAnioChart');
        return;
    }
    
    costoAnioChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Costo Proyectado de Reemplazo ($)',
                data: values,
                backgroundColor: '#667eea',
                borderRadius: 10
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                y: { 
                    beginAtZero: true, 
                    title: { display: true, text: 'Costo ($)' },
                    ticks: {
                        callback: function(value) {
                            return '$' + value.toLocaleString();
                        }
                    }
                }
            }
        }
    });
    console.log('✅ Gráfico costoAnioChart inicializado');
}

// Gráfico de puestos (CORREGIDO)
function initPuestosChart(labels, totales, listos) {
    console.log('initPuestosChart ejecutándose con:', {labels, totales, listos});
    
    const ctx = document.getElementById('puestosChart');
    if (!ctx) {
        console.error('No se encontró puestosChart');
        return;
    }
    
    if (puestosChart) {
        puestosChart.destroy();
        puestosChart = null;
    }
    
    if (!labels || labels.length === 0) {
        console.warn('No hay datos para puestosChart');
        ctx.style.display = 'none';
        const parent = ctx.parentElement;
        if (parent && !parent.querySelector('.no-data-msg')) {
            const msg = document.createElement('div');
            msg.className = 'alert alert-warning mt-2 no-data-msg';
            msg.innerHTML = '⚠️ No hay datos de sucesores por puesto. Verifica la base de datos.';
            parent.appendChild(msg);
        }
        return;
    }
    
    // Mostrar el canvas si estaba oculto
    ctx.style.display = 'block';
    
    try {
        puestosChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Total Sucesores Potenciales',
                        data: totales,
                        backgroundColor: '#667eea',
                        borderRadius: 10
                    },
                    {
                        label: 'Sucesores Listos',
                        data: listos,
                        backgroundColor: '#28a745',
                        borderRadius: 10
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                scales: {
                    y: { 
                        beginAtZero: true, 
                        title: { display: true, text: 'Cantidad' },
                        ticks: { stepSize: 1, precision: 0 }
                    },
                    x: { 
                        title: { display: true, text: 'Puestos Clave' },
                        ticks: { autoSkip: true, maxRotation: 45, minRotation: 45 }
                    }
                }
            }
        });
        console.log('✅ Gráfico puestosChart inicializado CORRECTAMENTE');
        puestosChart.update();
    } catch (error) {
        console.error('Error al crear puestosChart:', error);
    }
}

// Pilar 2: Gráfico de barras horizontal
function initBrechaDeptoChart(labels, brechas, totales, listos) {
    const ctx = document.getElementById('brechaDeptoChart');
    if (!ctx) return;
    
    if (brechaDeptoChart) {
        brechaDeptoChart.destroy();
        brechaDeptoChart = null;
    }
    
    if (!labels || labels.length === 0) return;
    
    brechaDeptoChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Brecha de Habilidad (%)',
                data: brechas,
                backgroundColor: function(context) {
                    const value = context.raw;
                    if (value > 60) return '#dc3545';
                    if (value > 30) return '#ffc107';
                    return '#28a745';
                }
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: 'y',
            scales: {
                x: { 
                    beginAtZero: true, 
                    max: 100,
                    title: { display: true, text: 'Brecha (%)' } 
                }
            }
        }
    });
    console.log('✅ Gráfico brechaDeptoChart inicializado');
}

// Pilar 3: Gráfico de dona (VERSIÓN SIMPLIFICADA - SIN PLUGINS)
function initDonaChart(conSucesor, sinSucesor) {
    console.log('initDonaChart ejecutándose:', {conSucesor, sinSucesor});
    
    const ctx = document.getElementById('donaChart');
    if (!ctx) {
        console.error('No se encontró donaChart');
        return;
    }
    
    if (donaChart) {
        donaChart.destroy();
        donaChart = null;
    }
    
    const total = (conSucesor || 0) + (sinSucesor || 0);
    const tasa = total > 0 ? ((conSucesor || 0) / total * 100).toFixed(1) : 0;
    
    try {
        donaChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['✅ Con sucesor listo', '❌ Sin sucesor listo'],
                datasets: [{
                    data: [conSucesor || 0, sinSucesor || 0],
                    backgroundColor: ['#28a745', '#dc3545'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                cutout: '60%',
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const value = context.raw || 0;
                                const pct = total > 0 ? (value / total * 100).toFixed(1) : 0;
                                return `${context.label}: ${value} (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
        
        // Agregar texto central manualmente (sin plugin)
        const originalDraw = donaChart.draw;
        donaChart.draw = function() {
            originalDraw.apply(this, arguments);
            const ctx = this.ctx;
            const width = this.canvas.width;
            const height = this.canvas.height;
            ctx.save();
            ctx.font = 'bold 20px "Segoe UI"';
            ctx.fillStyle = '#333';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(`${tasa}%`, width / 2, height / 2 - 5);
            ctx.font = '10px "Segoe UI"';
            ctx.fillStyle = '#666';
            ctx.fillText('Tasa sucesión', width / 2, height / 2 + 20);
            ctx.restore();
        };
        donaChart.draw();
        
        console.log('✅ Gráfico donaChart inicializado');
    } catch (error) {
        console.error('Error al crear donaChart:', error);
    }
}

// Inicialización principal
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM cargado, inicializando dashboard...');
    
    if (typeof window.dashboardData !== 'undefined') {
        const data = window.dashboardData;
        console.log('Datos recibidos:', data);
        
        // Pilar 1
        if (data.costo_anio && data.costo_anio.labels) {
            initCostoAnioChart(data.costo_anio.labels, data.costo_anio.values);
        }
        
        // Pilar 2
        if (data.departamentos && data.departamentos.labels) {
            initBrechaDeptoChart(
                data.departamentos.labels,
                data.departamentos.brechas,
                data.departamentos.totales,
                data.departamentos.listos
            );
        }
        
        // Pilar 3 (Dona)
        if (data.dona) {
            initDonaChart(data.dona.conSucesor, data.dona.sinSucesor);
        }
        
        // Gráfico de puestos
        if (data.puestos && data.puestos.labels && data.puestos.labels.length > 0) {
            console.log('Inicializando gráfico de puestos con:', data.puestos);
            initPuestosChart(
                data.puestos.labels, 
                data.puestos.totales, 
                data.puestos.listos
            );
        } else {
            console.warn('No hay datos de puestos para mostrar');
            const canvas = document.getElementById('puestosChart');
            if (canvas) {
                canvas.style.display = 'none';
                const parent = canvas.parentElement;
                if (parent && !parent.querySelector('.no-data-msg')) {
                    const msg = document.createElement('div');
                    msg.className = 'alert alert-warning mt-2 no-data-msg';
                    msg.innerHTML = '⚠️ No hay datos de sucesores por puesto. Verifica la base de datos.';
                    parent.appendChild(msg);
                }
            }
        }
    } else {
        console.error('No se encontró window.dashboardData');
    }
});