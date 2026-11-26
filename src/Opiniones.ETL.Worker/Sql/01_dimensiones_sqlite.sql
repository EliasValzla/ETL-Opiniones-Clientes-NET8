-- Dimensiones del Data Warehouse del Sistema de Analisis de Opiniones.
-- Esquema SQLite de demostracion; idempotente gracias a claves UNIQUE.
-- La tabla de hechos NO forma parte de esta entrega.
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS DimProducto (
    ProductoKey INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS DimCliente (
    ClienteKey INTEGER PRIMARY KEY AUTOINCREMENT,
    ClienteId TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS DimFecha (
    FechaKey INTEGER PRIMARY KEY,
    Fecha TEXT NOT NULL UNIQUE,
    Anio INTEGER NOT NULL,
    Trimestre INTEGER NOT NULL,
    Mes INTEGER NOT NULL,
    Dia INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS DimFuente (
    FuenteKey INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    Descripcion TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS DimSentimiento (
    SentimientoKey INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    Descripcion TEXT NOT NULL
);
