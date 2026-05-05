<?php
class Database {
    private $host = "ep-ancient-voice-anfnctl8-pooler.c-6.us-east-1.aws.neon.tech";
    private $port = "5432";
    private $db_name = "neondb";
    private $username = "lector_dw";
    private $password = "password_seguro";
    private $conn;

    public function getConnection() {
        try {
            $dsn = "pgsql:host={$this->host};port={$this->port};dbname={$this->db_name};sslmode=require";

            $this->conn = new PDO($dsn, $this->username, $this->password, [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
            ]);

            $this->conn->exec("SET search_path TO rh_dw, public");

            return $this->conn;

        } catch(PDOException $e) {
            error_log("Error DB: " . $e->getMessage());
            return null; // 🔥 NO romper la página
        }
    }
}