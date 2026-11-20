using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Configuration;

namespace Opiniones.ETL.Worker.Infrastructure;

// Datos inventados para la demostración. NO corresponden a opiniones reales.
public sealed class DatabaseDemoInitializer
{
    private readonly DatabaseOptions _op;
    private readonly IHostEnvironment _host;
    private readonly ILogger<DatabaseDemoInitializer> _logger;

    public DatabaseDemoInitializer(IOptions<DatabaseOptions> options,
        IHostEnvironment host, ILogger<DatabaseDemoInitializer> logger)
    {
        _op = options.Value;
        _host = host;
        _logger = logger;
    }

    public async Task InicializarAsync(CancellationToken ct)
    {
        if (!_op.Enabled || !_op.SeedDemo) return;
        var path = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.FilePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var db = new SqliteConnection($"Data Source={path}");
        await db.OpenAsync(ct);
        await using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Resenas (
                Id TEXT PRIMARY KEY, ProductoId TEXT NOT NULL, ClienteId TEXT NOT NULL,
                Texto TEXT NOT NULL, Calificacion INTEGER NOT NULL, FechaUtc TEXT NOT NULL
            );
            INSERT OR IGNORE INTO Resenas VALUES
                ('R001','P001','C006','Muy buen producto',5,'2026-10-06T00:00:00Z'),
                ('R002','P002','C007','La entrega pudo ser mejor',3,'2026-10-07T00:00:00Z'),
                ('R003','P003','C008','Material defectuoso',1,'2026-10-08T00:00:00Z');
            """;
        await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("Base de datos de ejemplo lista: {Path}", path);
    }
}
