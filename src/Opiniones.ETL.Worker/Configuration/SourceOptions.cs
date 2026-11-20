namespace Opiniones.ETL.Worker.Configuration;

public sealed class CsvOptions
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "Data/encuestas.csv";
}

public sealed class DatabaseOptions
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "Data/opiniones_web.db";
    public bool SeedDemo { get; set; } = true;
}

public sealed class ApiOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://127.0.0.1:5187/";
    public string Endpoint { get; set; } = "api/comentarios";
    public string ApiKey { get; set; } = "";
}

public sealed class StagingOptions
{
    public string Directory { get; set; } = "Staging";
}
