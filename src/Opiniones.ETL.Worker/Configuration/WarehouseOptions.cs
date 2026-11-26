namespace Opiniones.ETL.Worker.Configuration;

public sealed class WarehouseOptions
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "Data/opiniones_warehouse.db";
    public string SchemaPath { get; set; } = "Sql/01_dimensiones_sqlite.sql";
}
