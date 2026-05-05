<?php
// dashboard.php
require_once 'config/database.php';

$database = new Database();
$db = $database->getConnection();

// Consulta 1: Resumen general de sucesión
$query_resumen = "
    SELECT 
        COUNT(DISTINCT fs.EmpleadoKey) as total_sucesores,
       COUNT(DISTINCT CASE WHEN fs.FlagSucesorListo = TRUE THEN fs.EmpleadoKey END) as sucesores_listos,
        ROUND(AVG(fs.BrechaHabilidad), 2) as brecha_promedio,
        SUM(fs.CostoProyectadoReemplazo) as costo_total_reemplazo
    FROM Fact_Sucesion fs
    WHERE fs.TiempoKey = (SELECT MAX(TiempoKey) FROM Fact_Sucesion)
";

$stmt = $db->prepare($query_resumen);
$stmt->execute();
$resumen = $stmt->fetch(PDO::FETCH_ASSOC);

// Consulta 2: Sucesores por puesto
$query_puestos = "
    SELECT 
        dp.NombrePuesto,
        COUNT(DISTINCT fs.EmpleadoKey) as num_sucesores,
        SUM(CASE WHEN fs.FlagSucesorListo = TRUE THEN 1 ELSE 0 END) as listos,
        ROUND(AVG(fs.BrechaHabilidad), 2) as brecha_promedio
    FROM Fact_Sucesion fs
    JOIN DimPuesto dp ON fs.PuestoKey = dp.PuestoKey
    GROUP BY dp.NombrePuesto
    ORDER BY num_sucesores DESC
    LIMIT 10
";

$stmt = $db->prepare($query_puestos);
$stmt->execute();
$puestos = $stmt->fetchAll(PDO::FETCH_ASSOC);

// Consulta 3: Top empleados sucesores
$query_sucesores = "
    SELECT 
        de.NombreCompleto,
        dp.NombrePuesto as puesto_actual,
        fs.RolClaveCandidato,
        fs.BrechaHabilidad,
        fs.FlagSucesorListo,
        fs.CostoProyectadoReemplazo
    FROM Fact_Sucesion fs
    JOIN DimEmpleado de ON fs.EmpleadoKey = de.EmpleadoKey
    JOIN DimPuesto dp ON fs.PuestoKey = dp.PuestoKey
    WHERE de.EsRegistroActual = TRUE
    ORDER BY fs.BrechaHabilidad ASC
    LIMIT 20
";

$stmt = $db->prepare($query_sucesores);
$stmt->execute();
$sucesores = $stmt->fetchAll(PDO::FETCH_ASSOC);

// Consulta 4: Brecha por departamento
$query_departamentos = "
    SELECT 
        dd.NombreDepartamento,
        ROUND(AVG(fs.BrechaHabilidad), 2) as brecha_promedio,
        COUNT(*) as total_candidatos,
        SUM(CASE WHEN fs.FlagSucesorListo = TRUE THEN 1 ELSE 0 END) as listos
    FROM Fact_Sucesion fs
    JOIN DimDepartamento dd ON fs.DepartamentoKey = dd.DepartamentoKey
    GROUP BY dd.NombreDepartamento
    ORDER BY brecha_promedio DESC
";

$stmt = $db->prepare($query_departamentos);
$stmt->execute();
$departamentos = $stmt->fetchAll(PDO::FETCH_ASSOC);

// Consulta 5: Evolución temporal
$query_temporal = "
    SELECT 
        dt.Anio,
        dt.NombreMes,
        COUNT(*) as total_sucesores,
        SUM(CASE WHEN fs.FlagSucesorListo = TRUE THEN 1 ELSE 0 END) as listos
    FROM Fact_Sucesion fs
    JOIN DimTiempo dt ON fs.TiempoKey = dt.TiempoKey
    GROUP BY dt.Anio, dt.NombreMes, dt.Mes
    ORDER BY dt.Anio, dt.Mes DESC
    LIMIT 12
";

$stmt = $db->prepare($query_temporal);
$stmt->execute();
$temporal = $stmt->fetchAll(PDO::FETCH_ASSOC);
?>

<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dashboard de Sucesión - RRHH</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }

        .dashboard-container {
            max-width: 1400px;
            margin: 0 auto;
        }

        .header {
            background: white;
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 30px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.1);
        }

        .header h1 {
            color: #333;
            font-size: 28px;
            margin-bottom: 10px;
        }

        .header p {
            color: #666;
            font-size: 14px;
        }

        .stat-card {
            background: white;
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
            transition: transform 0.3s;
        }

        .stat-card:hover {
            transform: translateY(-5px);
        }

        .stat-card .icon {
            font-size: 40px;
            margin-bottom: 15px;
        }

        .stat-card .value {
            font-size: 32px;
            font-weight: bold;
            margin-bottom: 5px;
        }

        .stat-card .label {
            color: #666;
            font-size: 14px;
        }

        .stat-card.success .icon { color: #28a745; }
        .stat-card.warning .icon { color: #ffc107; }
        .stat-card.danger .icon { color: #dc3545; }
        .stat-card.info .icon { color: #17a2b8; }

        .chart-card {
            background: white;
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }

        .chart-card h3 {
            font-size: 18px;
            margin-bottom: 20px;
            color: #333;
        }

        .table-card {
            background: white;
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
            overflow-x: auto;
        }

        .table-card h3 {
            font-size: 18px;
            margin-bottom: 20px;
            color: #333;
        }

        table {
            width: 100%;
            border-collapse: collapse;
        }

        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }

        th {
            background: #f8f9fa;
            font-weight: 600;
            color: #555;
        }

        tr:hover {
            background: #f8f9fa;
        }

        .badge-success {
            background: #d4edda;
            color: #155724;
            padding: 5px 10px;
            border-radius: 20px;
            font-size: 12px;
        }

        .badge-warning {
            background: #fff3cd;
            color: #856404;
            padding: 5px 10px;
            border-radius: 20px;
            font-size: 12px;
        }

        .progress-bar-custom {
            width: 100%;
            height: 8px;
            background: #e0e0e0;
            border-radius: 10px;
            overflow: hidden;
        }

        .progress-fill {
            height: 100%;
            background: linear-gradient(90deg, #667eea, #764ba2);
            border-radius: 10px;
            transition: width 0.3s;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .fade-in {
            animation: fadeIn 0.5s ease-out;
        }
    </style>
</head>
<body>
    <div class="dashboard-container">
        <!-- Header -->
        <div class="header fade-in">
            <h1><i class="fas fa-chalkboard-user"></i> Dashboard de Planificación de Sucesión</h1>
            <p>Monitoreo de talento clave y preparación de sucesores | Actualizado: <?php echo date('d/m/Y H:i:s'); ?></p>
        </div>

        <!-- Stats Cards -->
        <div class="row fade-in">
            <div class="col-md-3">
                <div class="stat-card success">
                    <div class="icon"><i class="fas fa-users"></i></div>
                    <div class="value"><?php echo number_format($resumen['total_sucesores'] ?? 0); ?></div>
                    <div class="label">Total de Sucesores Identificados</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card warning">
                    <div class="icon"><i class="fas fa-briefcase"></i></div>
                    <div class="value"><?php echo number_format($resumen['total_puestos_clave'] ?? 0); ?></div>
                    <div class="label">Puestos Clave</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card success">
                    <div class="icon"><i class="fas fa-check-circle"></i></div>
                    <div class="value"><?php echo number_format($resumen['sucesores_listos'] ?? 0); ?></div>
                    <div class="label">Sucesores Listos</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card danger">
                    <div class="icon"><i class="fas fa-chart-line"></i></div>
                    <div class="value"><?php echo number_format($resumen['brecha_promedio'] ?? 0, 1); ?>%</div>
                    <div class="label">Brecha de Habilidad Promedio</div>
                </div>
            </div>
        </div>

        <!-- Charts Row 1 -->
        <div class="row fade-in">
            <div class="col-md-6">
                <div class="chart-card">
                    <h3><i class="fas fa-chart-bar"></i> Sucesores por Puesto</h3>
                    <canvas id="puestosChart"></canvas>
                </div>
            </div>
            <div class="col-md-6">
                <div class="chart-card">
                    <h3><i class="fas fa-chart-line"></i> Brecha por Departamento</h3>
                    <canvas id="departamentosChart"></canvas>
                </div>
            </div>
        </div>

        <!-- Charts Row 2 -->
        <div class="row fade-in">
            <div class="col-md-12">
                <div class="chart-card">
                    <h3><i class="fas fa-chart-line"></i> Evolución Temporal</h3>
                    <canvas id="temporalChart"></canvas>
                </div>
            </div>
        </div>

        <!-- Top Sucesores Table -->
        <div class="row fade-in">
            <div class="col-md-12">
                <div class="table-card">
                    <h3><i class="fas fa-trophy"></i> Top Sucesores por Preparación</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>Empleado</th>
                                <th>Puesto Actual</th>
                                <th>Rol Clave Candidato</th>
                                <th>Brecha Habilidad</th>
                                <th>Estado</th>
                                <th>Costo Reemplazo</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach($sucesores as $sucesor): ?>
                            <tr>
                                <td><?php echo htmlspecialchars($sucesor['nombrecompleto']); ?></td>
                                <td><?php echo htmlspecialchars($sucesor['puesto_actual']); ?></td>
                                <td><?php echo htmlspecialchars($sucesor['rolclavecandidato']); ?></td>
                                <td>
                                    <div class="progress-bar-custom">
                                        <div class="progress-fill" style="width: <?php echo $sucesor['brechahabilidad']; ?>%"></div>
                                    </div>
                                    <small><?php echo $sucesor['brechahabilidad']; ?>%</small>
                                </td>
                                <td>
                                    <?php if($sucesor['flagsucesorlisto']): ?>
                                        <span class="badge-success"><i class="fas fa-check"></i> Listo</span>
                                    <?php else: ?>
                                        <span class="badge-warning"><i class="fas fa-clock"></i> En Desarrollo</span>
                                    <?php endif; ?>
                                </td>
                                <td>$<?php echo number_format($sucesor['costoproyectadoreemplazo'], 2); ?></td>
                            </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <script>
        // Gráfico de Puestos
        const puestosCtx = document.getElementById('puestosChart').getContext('2d');
        new Chart(puestosCtx, {
            type: 'bar',
            data: {
                labels: <?php echo json_encode(array_column($puestos, 'nombrepuesto')); ?>,
                datasets: [{
                    label: 'Número de Sucesores',
                    data: <?php echo json_encode(array_column($puestos, 'num_sucesores')); ?>,
                    backgroundColor: 'rgba(102, 126, 234, 0.7)',
                    borderColor: 'rgba(102, 126, 234, 1)',
                    borderWidth: 1
                }, {
                    label: 'Sucesores Listos',
                    data: <?php echo json_encode(array_column($puestos, 'listos')); ?>,
                    backgroundColor: 'rgba(40, 167, 69, 0.7)',
                    borderColor: 'rgba(40, 167, 69, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'top' },
                    title: { display: false }
                },
                scales: {
                    y: { beginAtZero: true, title: { display: true, text: 'Cantidad' } },
                    x: { title: { display: true, text: 'Puestos' } }
                }
            }
        });

        // Gráfico de Departamentos
        const deptosCtx = document.getElementById('departamentosChart').getContext('2d');
        new Chart(deptosCtx, {
            type: 'horizontalBar',
            data: {
                labels: <?php echo json_encode(array_column($departamentos, 'nombredepartamento')); ?>,
                datasets: [{
                    label: 'Brecha de Habilidad (%)',
                    data: <?php echo json_encode(array_column($departamentos, 'brecha_promedio')); ?>,
                    backgroundColor: 'rgba(118, 75, 162, 0.7)',
                    borderColor: 'rgba(118, 75, 162, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                indexAxis: 'y',
                plugins: {
                    legend: { position: 'top' }
                },
                scales: {
                    x: { 
                        beginAtZero: true, 
                        max: 100,
                        title: { display: true, text: 'Brecha de Habilidad (%)' }
                    }
                }
            }
        });

        // Gráfico Temporal
        const temporalCtx = document.getElementById('temporalChart').getContext('2d');
        new Chart(temporalCtx, {
            type: 'line',
            data: {
                labels: <?php echo json_encode(array_map(function($item) {
                    return $item['anio'] . ' - ' . $item['nombremes'];
                }, $temporal)); ?>,
                datasets: [{
                    label: 'Total Sucesores',
                    data: <?php echo json_encode(array_column($temporal, 'total_sucesores')); ?>,
                    borderColor: 'rgba(102, 126, 234, 1)',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    tension: 0.4,
                    fill: true
                }, {
                    label: 'Sucesores Listos',
                    data: <?php echo json_encode(array_column($temporal, 'listos')); ?>,
                    borderColor: 'rgba(40, 167, 69, 1)',
                    backgroundColor: 'rgba(40, 167, 69, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'top' }
                },
                scales: {
                    y: { beginAtZero: true, title: { display: true, text: 'Cantidad' } }
                }
            }
        });
    </script>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>