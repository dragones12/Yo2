<?php
// index.php - Dashboard principal del Módulo 8
// Planificación de la Fuerza Laboral - 4 KPIs + 3 Pilares

require_once 'queries/sucesion_queries.php';

$queries = new SucesionQueries();

// ============================================================
// 4 KPIs OFICIALES
// ============================================================
$kpi_costo_total = $queries->getKpiCostoTotalReemplazo();
$kpi_brecha_promedio = $queries->getKpiBrechaPromedio();
$kpi_tasa_sucesion = $queries->getKpiTasaSucesion();
$kpi_puestos_sin_sucesor = $queries->getKpiPuestosSinSucesor();

// ============================================================
// DATOS PARA GRÁFICOS
// ============================================================
$costo_por_anio = $queries->getCostoPorAnio();
$departamentos = $queries->getBrechaPorDepartamento();
$dona_data = $queries->getDonaSucesion();
$puestos = $queries->getSucesoresPorPuesto();  // ← Datos para gráfico de puestos
$tabla_riesgo = $queries->getTablaPuestosEnRiesgo();
$jubilaciones_riesgo = $queries->getJubilacionesRiesgo();
$ultima_fecha = $queries->getUltimaFecha();

// Procesar dona
$con_sucesor = 0;
$sin_sucesor = 0;
foreach ($dona_data as $item) {
    if ($item['estado_sucesion'] == 'Con sucesor listo') {
        $con_sucesor = $item['cantidad'];
    } else {
        $sin_sucesor = $item['cantidad'];
    }
}

// Nivel de riesgo
if ($kpi_tasa_sucesion >= 70) {
    $nivel_riesgo = "Bajo";
    $color_riesgo = "#28a745";
    $mensaje_riesgo = "✅ La empresa está bien preparada para cubrir puestos clave.";
} elseif ($kpi_tasa_sucesion >= 40) {
    $nivel_riesgo = "Medio";
    $color_riesgo = "#ffc107";
    $mensaje_riesgo = "⚠️ Hay áreas de mejora. Enfocar esfuerzos en los puestos críticos con mayor brecha.";
} else {
    $nivel_riesgo = "Alto";
    $color_riesgo = "#dc3545";
    $mensaje_riesgo = "🔴 ¡URGENTE! La empresa no está preparada para reemplazar puestos clave.";
}

// ============================================================
// DEBUG: Verificar datos de puestos (opcional, quitar en producción)
// ============================================================
// echo '<pre>'; print_r($puestos); echo '</pre>';

// Preparar datos para JavaScript - CORREGIDO
$dashboardData = [
    'costo_anio' => [
        'labels' => array_column($costo_por_anio, 'anio'),
        'values' => array_column($costo_por_anio, 'costo_total')
    ],
    'departamentos' => [
        'labels' => array_column($departamentos, 'nombredepartamento'),
        'brechas' => array_column($departamentos, 'brecha_promedio'),
        'totales' => array_column($departamentos, 'total_candidatos'),
        'listos' => array_column($departamentos, 'sucesores_listos')
    ],
    'dona' => [
        'conSucesor' => $con_sucesor,
        'sinSucesor' => $sin_sucesor
    ],
    'puestos' => [
        'labels' => array_column($puestos, 'nombrepuesto'),
        'totales' => array_column($puestos, 'total_sucesores'),
        'listos' => array_column($puestos, 'listos')
    ]
];
?>

<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Workforce Planning | Dashboard de Sucesión</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link rel="stylesheet" href="assets/css/styles.css">
</head>
<body>

<div class="dashboard-container">

    <!-- HEADER -->
    <div class="header fade-in">
        <h1><i class="fas fa-chalkboard-user"></i> Módulo 8: Planificación de la Fuerza Laboral</h1>
        <p>
            <i class="fas fa-question-circle"></i> <strong>Objetivo:</strong> Prever necesidades futuras de personal, identificar brechas de habilidades y planificar la sucesión de roles clave.<br>
            <i class="fas fa-chart-line"></i> <strong>Tres pilares:</strong> Prever personal | Brechas de habilidades | Sucesión de roles<br>
            <i class="fas fa-calendar"></i> <strong>Actualizado:</strong> <?php echo date('d/m/Y H:i:s'); ?> | 
            <strong>Período:</strong> <?php echo $ultima_fecha; ?>
        </p>
    </div>

    <!-- FILA 1: 4 KPIs -->
    <div class="row fade-in">
        <div class="col-md-3">
            <div class="kpi-card primary">
                <div class="icon"><i class="fas fa-dollar-sign"></i></div>
                <div class="value">$<?php echo number_format($kpi_costo_total, 0); ?></div>
                <div class="label">Costo Total Proyectado Reemplazo</div>
                <div class="subtext">Kpi 1 - Prever personal</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="kpi-card warning">
                <div class="icon"><i class="fas fa-chart-line"></i></div>
                <div class="value"><?php echo $kpi_brecha_promedio; ?>%</div>
                <div class="label">Brecha Promedio de Habilidades</div>
                <div class="subtext">Kpi 2 - Identificar brechas</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="kpi-card success">
                <div class="icon"><i class="fas fa-percent"></i></div>
                <div class="value"><?php echo $kpi_tasa_sucesion; ?>%</div>
                <div class="label">Tasa de Sucesión</div>
                <div class="subtext">Kpi 3 - Sucesión de roles</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="kpi-card danger">
                <div class="icon"><i class="fas fa-exclamation-triangle"></i></div>
                <div class="value"><?php echo $kpi_puestos_sin_sucesor; ?></div>
                <div class="label">Puestos Clave Sin Sucesor</div>
                <div class="subtext">Kpi 4 - Riesgo directo</div>
            </div>
        </div>
    </div>

    <!-- FILA 2: GRÁFICOS POR PILAR -->
    <div class="row fade-in">
        <div class="col-md-4">
            <div class="chart-card">
                <h3><i class="fas fa-coins"></i> Pilar 1 - Costo Proyectado por Año</h3>
                <canvas id="costoAnioChart" style="height: 250px; width: 100%;"></canvas>
                <p class="text-muted mt-2 small">Costo total de reemplazo por año</p>
            </div>
        </div>
        <div class="col-md-4">
            <div class="chart-card">
                <h3><i class="fas fa-building"></i> Pilar 2 - Brecha por Departamento</h3>
                <canvas id="brechaDeptoChart" style="height: 250px; width: 100%;"></canvas>
                <p class="text-muted mt-2 small">🟢 &lt;30% | 🟡 30-60% | 🔴 &gt;60%</p>
            </div>
        </div>
        <div class="col-md-4">
            <div class="chart-card">
                <h3><i class="fas fa-chart-pie"></i> Pilar 3 - Estado de Sucesión</h3>
                <div class="dona-container">
                    <canvas id="donaChart" style="height: 200px; width: 100%;"></canvas>
                </div>
                <p class="text-muted mt-2 small text-center">Tasa actual: <strong><?php echo $kpi_tasa_sucesion; ?>%</strong></p>
            </div>
        </div>
    </div>

    <!-- FILA 3: Sucesores por Puesto Clave (GRÁFICO CORREGIDO) -->
    <div class="row fade-in">
        <div class="col-md-12">
            <div class="chart-card">
                <h3><i class="fas fa-briefcase"></i> Sucesores por Puesto Clave</h3>
                <canvas id="puestosChart" style="height: 300px; width: 100%;"></canvas>
                <p class="text-muted mt-2 small">📌 Azul = total candidatos | Verde = listos para asumir</p>
                <?php if (empty($puestos)): ?>
                    <div class="alert alert-warning mt-2">⚠️ No hay datos de puestos para mostrar.</div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- FILA 4: Tabla de Riesgo -->
    <div class="row fade-in">
        <div class="col-md-12">
            <div class="table-card">
                <h3><i class="fas fa-list-ul"></i> Puestos Críticos en Riesgo (sin sucesor listo)</h3>
                <div class="table-responsive">
                    <table class="table">
                        <thead>
                            <tr><th>Empleado</th><th>Puesto Actual</th><th>Rol Clave</th><th>Brecha</th><th>Costo Reemplazo</th><th>Riesgo</th></tr>
                        </thead>
                        <tbody>
                            <?php if (count($tabla_riesgo) > 0): ?>
                                <?php foreach ($tabla_riesgo as $row): ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($row['empleado'] ?? 'N/A'); ?></td>
                                    <td><?php echo htmlspecialchars($row['puesto_actual'] ?? 'N/A'); ?></td>
                                    <td><span class="badge-danger" style="padding:3px 8px;"><?php echo htmlspecialchars($row['rol_clave'] ?? 'N/A'); ?></span></td>
                                    <td style="min-width:120px">
                                        <div class="progress-bar-custom"><div class="progress-fill" style="width:<?php echo $row['brecha'] ?? 0; ?>%; background:<?php echo ($row['brecha'] ?? 0) > 60 ? '#dc3545' : (($row['brecha'] ?? 0) > 30 ? '#ffc107' : '#28a745'); ?>"></div></div>
                                        <small><?php echo number_format($row['brecha'] ?? 0, 1); ?>%</small>
                                    </td>
                                    <td>$<?php echo number_format($row['costo_reemplazo'] ?? 0, 0); ?></td>
                                    <td><?php echo (($row['brecha'] ?? 0) > 60) ? '<span class="badge-danger">🔴 ALTO</span>' : ((($row['brecha'] ?? 0) > 30) ? '<span class="badge-warning">🟡 MEDIO</span>' : '<span class="badge-success">🟢 BAJO</span>'); ?></td>
                                </tr>
                                <?php endforeach; ?>
                            <?php else: ?>
                                <tr><td colspan="6" class="text-center">✅ No hay puestos críticos en riesgo</td></tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- FILA 5: Conclusiones -->
    <div class="row fade-in">
        <div class="col-md-12">
            <div class="conclusion-card">
                <h3><i class="fas fa-clipboard-list"></i> Diagnóstico y Recomendaciones</h3>
                <p><strong>Nivel de riesgo:</strong> <span class="riesgo-badge" style="background:<?php echo $color_riesgo; ?>;color:white"><?php echo $nivel_riesgo; ?></span></p>
                <p><?php echo $mensaje_riesgo; ?></p>
                <hr>
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>Hallazgos clave:</strong></p>
                        <ul>
                            <li>📈 <strong><?php echo $kpi_tasa_sucesion; ?>%</strong> de puestos críticos tienen sucesor listo</li>
                            <li>📊 Brecha promedio: <strong><?php echo $kpi_brecha_promedio; ?>%</strong></li>
                            <li>⚠️ <strong><?php echo $kpi_puestos_sin_sucesor; ?></strong> puestos en riesgo</li>
                            <li>👴 <strong><?php echo $jubilaciones_riesgo; ?></strong> empleados con alto riesgo de jubilación sin sucesor</li>
                            <li>💰 Costo potencial: <strong>$<?php echo number_format($kpi_costo_total, 0); ?></strong></li>
                        </ul>
                    </div>
                    <div class="col-md-6">
                        <p><strong>Recomendaciones:</strong></p>
                        <ul>
                            <?php if ($kpi_brecha_promedio > 60): ?>
                                <li>🔴 Capacitación intensiva para reducir brecha</li>
                                <li>🎯 Reclutamiento externo para puestos críticos</li>
                            <?php elseif ($kpi_brecha_promedio > 30): ?>
                                <li>🟡 Programas de mentoring</li>
                                <li>📅 Revisiones trimestrales del plan</li>
                            <?php else: ?>
                                <li>🟢 Mantener y monitorear el plan actual</li>
                                <li>📈 Retención de talento clave</li>
                            <?php endif; ?>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    </div>

</div>

<script>
    window.dashboardData = <?php echo json_encode($dashboardData); ?>;
    // Debug: Ver en consola qué datos llegan
    console.log('Dashboard Data:', window.dashboardData);
</script>
<script src="assets/js/dashboard.js"></script>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>