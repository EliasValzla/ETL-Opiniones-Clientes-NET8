using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Infrastructure;

public sealed class CsvExtractor : IExtractor
{
    private readonly CsvOptions _op;
    private readonly IHostEnvironment _host;

    public CsvExtractor(IOptions<CsvOptions> options, IHostEnvironment host)
    {
        _op = options.Value;
        _host = host;
    }

    public string Nombre => "encuestas_csv";
    public bool Habilitado => _op.Enabled;

    public async IAsyncEnumerable<OpinionCruda> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.FilePath));
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el CSV de encuestas.", path);

        using var file = new StreamReader(path);
        using var csv = new CsvReader(file, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            TrimOptions = TrimOptions.Trim
        });

        await foreach (var fila in csv.GetRecordsAsync<EncuestaCsv>(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(fila.IdEncuesta) ||
                string.IsNullOrWhiteSpace(fila.ProductoId) ||
                string.IsNullOrWhiteSpace(fila.Comentario))
                throw new InvalidDataException("CSV: hay campos obligatorios vacíos.");

            if (!int.TryParse(fila.Calificacion, out var nota) || nota is < 1 or > 5)
                throw new InvalidDataException($"CSV: calificación inválida para {fila.IdEncuesta}.");

            if (!DateTime.TryParseExact(fila.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fecha))
                throw new InvalidDataException($"CSV: fecha inválida para {fila.IdEncuesta}.");

            yield return new OpinionCruda("csv", fila.IdEncuesta, fila.ProductoId,
                fila.ClienteId, fila.Comentario, nota, fecha);
        }
    }

    public sealed class EncuestaCsv
    {
        public string IdEncuesta { get; set; } = "";
        public string ProductoId { get; set; } = "";
        public string ClienteId { get; set; } = "";
        public string Comentario { get; set; } = "";
        public string Calificacion { get; set; } = "";
        public string Fecha { get; set; } = "";
    }
}
