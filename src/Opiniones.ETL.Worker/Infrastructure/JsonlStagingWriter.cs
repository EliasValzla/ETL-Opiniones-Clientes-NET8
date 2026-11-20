using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;

namespace Opiniones.ETL.Worker.Infrastructure;

public sealed class JsonlStagingWriter : IStagingWriter
{
    private readonly StagingOptions _op;
    private readonly IHostEnvironment _host;

    public JsonlStagingWriter(IOptions<StagingOptions> options, IHostEnvironment host)
    {
        _op = options.Value;
        _host = host;
    }

    public async Task<(int Registros, string Ruta)> GuardarAsync(IExtractor extractor,
        CancellationToken ct)
    {
        var dir = Path.GetFullPath(Path.Combine(_host.ContentRootPath, _op.Directory));
        Directory.CreateDirectory(dir);
        var nombre = $"{extractor.Nombre}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.jsonl";
        var final = Path.Combine(dir, nombre);
        var temporal = final + ".tmp";
        var conteo = 0;

        try
        {
            await using (var stream = new FileStream(temporal, new FileStreamOptions
            {
                Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
                Options = FileOptions.Asynchronous, BufferSize = 65536
            }))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 65536))
            {
                await foreach (var registro in extractor.ExtraerAsync(ct).WithCancellation(ct))
                {
                    var json = JsonSerializer.Serialize(registro);
                    await writer.WriteLineAsync(json.AsMemory(), ct);
                    conteo++;
                }
            }

            File.Move(temporal, final);
            return (conteo, final);
        }
        catch
        {
            if (File.Exists(temporal)) File.Delete(temporal);
            throw;
        }
    }
}
