using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Infrastructure;

/// <summary>
/// Carga solo las dimensiones del almacén analítico a partir de los JSONL
/// producidos EN ESTA ejecución. Mantiene una transacción por corrida.
/// No crea la tabla de hechos ni simula su carga.
/// </summary>
public sealed class SqliteDimensionLoader : IDimensionLoader
{
    private readonly WarehouseOptions _op;
    private readonly IHostEnvironment _host;
    private readonly ILogger<SqliteDimensionLoader> _logger;

    public SqliteDimensionLoader(IOptions<WarehouseOptions> options,
        IHostEnvironment host, ILogger<SqliteDimensionLoader> logger)
    {
        _op = options.Value;
        _host = host;
        _logger = logger;
    }

    public async Task<DimensionLoadResult> CargarAsync(IReadOnlyCollection<string> rutasStaging,
        CancellationToken cancellationToken)
    {
        if (rutasStaging.Count == 0)
            throw new InvalidOperationException("La carga dimensional necesita archivos staging.");

        var destino = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.FilePath));
        var schemaPath = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.SchemaPath));
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("No se encontró el script de dimensiones.", schemaPath);

        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using var conexion = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destino
        }.ToString());
        await conexion.OpenAsync(cancellationToken);
        await using var transaccion = (SqliteTransaction)await conexion.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var esquema = conexion.CreateCommand())
            {
                esquema.Transaction = transaccion;
                esquema.CommandText = await File.ReadAllTextAsync(schemaPath, cancellationToken);
                await esquema.ExecuteNonQueryAsync(cancellationToken);
            }

            var procesadas = 0;
            foreach (var ruta in rutasStaging.OrderBy(x => x, StringComparer.Ordinal))
            {
                await using var archivo = new FileStream(ruta, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: 65536, useAsync: true);
                using var lector = new StreamReader(archivo);
                string? linea;
                while ((linea = await lector.ReadLineAsync(cancellationToken)) is not null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;
                    var opinion = JsonSerializer.Deserialize<OpinionCruda>(linea)
                        ?? throw new InvalidDataException($"Registro JSON vacío en {ruta}.");
                    Validar(opinion);
                    await GuardarDimensionProducto(conexion, transaccion, opinion.ProductoId.Trim().ToUpperInvariant(), cancellationToken);
                    await GuardarDimensionCliente(conexion, transaccion, opinion.ClienteId.Trim().ToUpperInvariant(), cancellationToken);
                    await GuardarDimensionFecha(conexion, transaccion, opinion.FechaUtc, cancellationToken);
                    await GuardarDimensionFuente(conexion, transaccion, opinion.Fuente.Trim().ToLowerInvariant(), cancellationToken);
                    await GuardarDimensionSentimiento(conexion, transaccion, opinion.Calificacion, cancellationToken);
                    procesadas++;
                }
            }

            // Leer antes del COMMIT garantiza que los conteos coincidan con la
            // misma transacción; si algo falla, se revierte toda la carga.
            var resultado = new DimensionLoadResult(
                procesadas,
                await Contar(conexion, transaccion, "DimProducto", cancellationToken),
                await Contar(conexion, transaccion, "DimCliente", cancellationToken),
                await Contar(conexion, transaccion, "DimFecha", cancellationToken),
                await Contar(conexion, transaccion, "DimFuente", cancellationToken),
                await Contar(conexion, transaccion, "DimSentimiento", cancellationToken),
                destino);

            await transaccion.CommitAsync(cancellationToken);
            _logger.LogInformation("Carga de dimensiones confirmada en SQLite: {Ruta}", destino);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void Validar(OpinionCruda opinion)
    {
        if (string.IsNullOrWhiteSpace(opinion.Fuente) ||
            string.IsNullOrWhiteSpace(opinion.IdOrigen) ||
            string.IsNullOrWhiteSpace(opinion.ProductoId) ||
            string.IsNullOrWhiteSpace(opinion.ClienteId))
            throw new InvalidDataException("No se puede cargar una dimensión con identificadores vacíos.");
        if (opinion.FechaUtc == default)
            throw new InvalidDataException("Fecha vacía en una opinión.");
        if (opinion.Calificacion is < 1 or > 5)
            throw new InvalidDataException("La calificación debe estar entre 1 y 5.");
    }

    private static Task<int> GuardarDimensionProducto(SqliteConnection db, SqliteTransaction tx,
        string id, CancellationToken ct) => Ejecutar(db, tx,
        "INSERT OR IGNORE INTO DimProducto (ProductoId) VALUES ($v);", ct, ("$v", id));

    private static Task<int> GuardarDimensionCliente(SqliteConnection db, SqliteTransaction tx,
        string id, CancellationToken ct) => Ejecutar(db, tx,
        "INSERT OR IGNORE INTO DimCliente (ClienteId) VALUES ($v);", ct, ("$v", id));

    private static async Task GuardarDimensionFecha(SqliteConnection db, SqliteTransaction tx,
        DateTime fechaUtc, CancellationToken ct)
    {
        var fecha = fechaUtc.ToUniversalTime().Date;
        var clave = fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
        await Ejecutar(db, tx,
            "INSERT OR IGNORE INTO DimFecha (FechaKey, Fecha, Anio, Trimestre, Mes, Dia) " +
            "VALUES ($key, $fecha, $anio, $trimestre, $mes, $dia);", ct,
            ("$key", clave), ("$fecha", fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("$anio", fecha.Year), ("$trimestre", (fecha.Month - 1) / 3 + 1),
            ("$mes", fecha.Month), ("$dia", fecha.Day));
    }

    private static Task<int> GuardarDimensionFuente(SqliteConnection db, SqliteTransaction tx,
        string codigo, CancellationToken ct)
    {
        var descripcion = codigo switch
        {
            "csv" => "Encuestas internas (CSV)",
            "base_datos" => "Reseñas web (BD relacional)",
            "api_rest" => "Comentarios externos (API REST)",
            _ => codigo
        };
        return Ejecutar(db, tx,
            "INSERT OR IGNORE INTO DimFuente (Codigo, Descripcion) VALUES ($c, $d);", ct,
            ("$c", codigo), ("$d", descripcion));
    }

    private static Task<int> GuardarDimensionSentimiento(SqliteConnection db, SqliteTransaction tx,
        int? calificacion, CancellationToken ct)
    {
        // Regla explícita del ejemplo (no es un clasificador NLP).
        var (codigo, descripcion) = calificacion switch
        {
            >= 4 and <= 5 => ("POSITIVO", "Calificación 4 o 5"),
            3 => ("NEUTRO", "Calificación 3"),
            >= 1 and <= 2 => ("NEGATIVO", "Calificación 1 o 2"),
            null => ("SIN_NOTA", "Sin calificación"),
            _ => throw new InvalidDataException("Calificación fuera del rango 1-5")
        };
        return Ejecutar(db, tx,
            "INSERT OR IGNORE INTO DimSentimiento (Codigo, Descripcion) VALUES ($c, $d);", ct,
            ("$c", codigo), ("$d", descripcion));
    }

    private static async Task<int> Ejecutar(SqliteConnection db, SqliteTransaction tx,
        string sql, CancellationToken ct, params (string Nombre, object Valor)[] parametros)
    {
        await using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre, valor);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> Contar(SqliteConnection db, SqliteTransaction tx,
        string tabla, CancellationToken ct)
    {
        // Nombres fijos suministrados por nuestro código, no por el usuario.
        await using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"SELECT COUNT(*) FROM {tabla}";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }
}
