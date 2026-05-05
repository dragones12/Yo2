<?php
// queries/sucesion_queries.php - TODAS las consultas SQL del módulo 8
// 4 KPIs oficiales + gráficos por pilar

require_once __DIR__ . '/../config/database.php';

class SucesionQueries
{
    private $db;
    private $ultima_fecha;

    public function __construct()
    {
        $database = new Database();
        $this->db = $database->getConnection();
        $this->cargarUltimaFecha();
    }

    private function cargarUltimaFecha()
    {
        $query = "SELECT MAX(TiempoKey) as ultima_fecha FROM Fact_Sucesion";
        $stmt = $this->db->prepare($query);
        $stmt->execute();
        $this->ultima_fecha = $stmt->fetch(PDO::FETCH_ASSOC)['ultima_fecha'];
    }

    // ============================================================
    // 4 KPIS OFICIALES DEL MÓDULO DE SUCESIÓN
    // ============================================================

    // KPI N°1: Costo Total Proyectado de Reemplazo de Personal (PILAR 1)
    public function getKpiCostoTotalReemplazo()
    {
        $query = "
            SELECT COALESCE(SUM(CostoProyectadoReemplazo), 0) AS costo_total
            FROM Fact_Sucesion
            WHERE TiempoKey = :ultima_fecha
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        $result = $stmt->fetch(PDO::FETCH_ASSOC);
        return round($result['costo_total'] ?? 0, 2);
    }

    // KPI N°2: Brecha Promedio de Habilidades (PILAR 2)
    public function getKpiBrechaPromedio()
    {
        $query = "
            SELECT ROUND(COALESCE(AVG(BrechaHabilidad), 0), 2) AS brecha_promedio
            FROM Fact_Sucesion
            WHERE TiempoKey = :ultima_fecha
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        $result = $stmt->fetch(PDO::FETCH_ASSOC);
        return round($result['brecha_promedio'] ?? 0, 1);
    }

    // KPI N°3: Tasa de Sucesión de Puestos Clave (PILAR 3)
    public function getKpiTasaSucesion()
    {
        $query = "
            SELECT 
                ROUND(
                    COUNT(CASE WHEN FlagSucesorListo = TRUE THEN 1 END) * 100.0 / 
                    NULLIF(COUNT(*), 0), 
                    2
                ) AS tasa_sucesion
            FROM Fact_Sucesion
            WHERE TiempoKey = :ultima_fecha
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        $result = $stmt->fetch(PDO::FETCH_ASSOC);
        return round($result['tasa_sucesion'] ?? 0, 1);
    }

    // KPI N°4: Número de Puestos Clave Sin Sucesor Identificado (PILAR 3)
    public function getKpiPuestosSinSucesor()
    {
        $query = "
            SELECT COUNT(CASE WHEN FlagSucesorListo = FALSE THEN 1 END) AS puestos_sin_sucesor
            FROM Fact_Sucesion
            WHERE TiempoKey = :ultima_fecha
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        $result = $stmt->fetch(PDO::FETCH_ASSOC);
        return $result['puestos_sin_sucesor'] ?? 0;
    }

    // ============================================================
    // GRÁFICOS POR PILAR
    // ============================================================

    // Pilar 1 - Gráfico: Costo proyectado total por año
    public function getCostoPorAnio()
    {
        $query = "
            SELECT 
                dt.Anio,
                COALESCE(SUM(fs.CostoProyectadoReemplazo), 0) AS costo_total
            FROM Fact_Sucesion fs
            JOIN DimTiempo dt ON fs.TiempoKey = dt.TiempoKey
            GROUP BY dt.Anio
            ORDER BY dt.Anio
        ";
        $stmt = $this->db->prepare($query);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Pilar 2 - Gráfico: Brecha promedio por departamento (ordenado mayor a menor)
    public function getBrechaPorDepartamento()
    {
        $query = "
            SELECT 
                dd.NombreDepartamento,
                ROUND(COALESCE(AVG(fs.BrechaHabilidad), 0), 1) AS brecha_promedio,
                COUNT(*) AS total_candidatos,
                COUNT(CASE WHEN fs.FlagSucesorListo = TRUE THEN 1 END) AS sucesores_listos
            FROM Fact_Sucesion fs
            JOIN DimDepartamento dd ON fs.DepartamentoKey = dd.DepartamentoKey
            WHERE fs.TiempoKey = :ultima_fecha
            GROUP BY dd.NombreDepartamento
            ORDER BY brecha_promedio DESC
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Pilar 3 - Gráfico: Conteo para gráfico de dona (Con sucesor vs Sin sucesor)
    public function getDonaSucesion()
    {
        $query = "
            SELECT 
                CASE 
                    WHEN FlagSucesorListo = TRUE THEN 'Con sucesor listo'
                    ELSE 'Sin sucesor listo'
                END AS estado_sucesion,
                COUNT(*) AS cantidad
            FROM Fact_Sucesion
            WHERE TiempoKey = :ultima_fecha
            GROUP BY FlagSucesorListo
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Gráfico complementario: Sucesores por puesto (top 10)
    public function getSucesoresPorPuesto()
    {
        $query = "
        SELECT 
            dp.NombrePuesto,
            COUNT(*) AS total_sucesores,
            COUNT(CASE WHEN fs.FlagSucesorListo = TRUE THEN 1 END) AS listos,
            ROUND(AVG(fs.BrechaHabilidad), 1) AS brecha_promedio
        FROM Fact_Sucesion fs
        JOIN DimPuesto dp ON fs.PuestoKey = dp.PuestoKey
        WHERE fs.TiempoKey = :ultima_fecha
        GROUP BY dp.NombrePuesto
        ORDER BY total_sucesores DESC
        LIMIT 10
    ";

        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();

        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
    // Tabla: Puestos en riesgo (sin sucesor listo)
    public function getTablaPuestosEnRiesgo()
    {
        $query = "
            SELECT 
                de.NombreCompleto AS empleado,
                dp.NombrePuesto AS puesto_actual,
                fs.RolClaveCandidato AS rol_clave,
                fs.BrechaHabilidad AS brecha,
                fs.CostoProyectadoReemplazo AS costo_reemplazo
            FROM Fact_Sucesion fs
            JOIN DimEmpleado de ON fs.EmpleadoKey = de.EmpleadoKey
            JOIN DimPuesto dp ON fs.PuestoKey = dp.PuestoKey
            WHERE fs.FlagSucesorListo = FALSE 
              AND fs.TiempoKey = :ultima_fecha
              AND de.EsRegistroActual = TRUE
            ORDER BY fs.BrechaHabilidad DESC
            LIMIT 20
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    // Pronóstico de jubilaciones (riesgo futuro)
    public function getJubilacionesRiesgo()
    {
        $query = "
            SELECT COUNT(*) AS total
            FROM Fact_Sucesion
            WHERE FlagSucesorListo = FALSE 
              AND BrechaHabilidad > 60
              AND TiempoKey = :ultima_fecha
        ";
        $stmt = $this->db->prepare($query);
        $stmt->bindParam(':ultima_fecha', $this->ultima_fecha);
        $stmt->execute();
        $result = $stmt->fetch(PDO::FETCH_ASSOC);
        return $result['total'] ?? 0;
    }

    // Obtener la última fecha
    public function getUltimaFecha()
    {
        return $this->ultima_fecha;
    }
}
