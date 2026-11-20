using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Infrastructure;

public sealed class DatabaseExtractor : IExtractor
{
    private readonly DatabaseOptions _op;
    private readonly IHostEnvironment _host;

    public DatabaseExtractor(IOptions<DatabaseOptions> options, IHostEnvironment host)
    {
        _op = options.Value;
        _host = host;
    }

    public string Nombre => "resenas_bd";
    public bool Habilitado => _op.Enabled;

    public async IAsyncEnumerable<OpinionCruda> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.FilePath));
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró la base de datos de reseñas.", path);

        await using var db = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        await db.OpenAsync(cancellationToken);
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Id, ProductoId, ClienteId, Texto, Calificacion, FechaUtc FROM Resenas ORDER BY Id";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var fecha = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            yield return new OpinionCruda("base_datos", reader.GetString(0),
                reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt32(4), fecha);
        }
    }
}
