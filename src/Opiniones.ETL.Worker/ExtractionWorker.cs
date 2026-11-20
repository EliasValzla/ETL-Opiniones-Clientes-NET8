using System.Diagnostics;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Infrastructure;

namespace Opiniones.ETL.Worker;

public sealed class ExtractionWorker : BackgroundService
{
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly IStagingWriter _staging;
    private readonly DatabaseDemoInitializer _demo;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(IEnumerable<IExtractor> extractores, IStagingWriter staging,
        DatabaseDemoInitializer demo, IHostApplicationLifetime lifetime,
        ILogger<ExtractionWorker> logger)
    {
        _extractores = extractores;
        _staging = staging;
        _demo = demo;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var total = Stopwatch.StartNew();
        try
        {
            await _demo.InicializarAsync(stoppingToken);
            var activos = _extractores.Where(x => x.Habilitado).ToArray();
            _logger.LogInformation("Iniciando extracción en {Cantidad} fuentes.", activos.Length);

            // Cada fuente tiene su propio archivo staging; no existe escritura concurrente al mismo archivo.
            var resultados = await Task.WhenAll(activos.Select(async extractor =>
            {
                var reloj = Stopwatch.StartNew();
                var resultado = await _staging.GuardarAsync(extractor, stoppingToken);
                _logger.LogInformation("Fuente {Fuente}: {Registros} registros en {Tiempo} ms. Staging: {Ruta}",
                    extractor.Nombre, resultado.Registros, reloj.ElapsedMilliseconds, resultado.Ruta);
                return resultado.Registros;
            }));

            _logger.LogInformation("Extracción FINALIZADA: {Registros} registros en {Tiempo} ms.",
                resultados.Sum(), total.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Extracción cancelada.");
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            _logger.LogError(ex, "Error en la extracción. Revise la fuente y la configuración.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
