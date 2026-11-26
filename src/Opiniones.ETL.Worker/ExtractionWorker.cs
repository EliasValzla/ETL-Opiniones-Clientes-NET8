using System.Diagnostics;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Infrastructure;

namespace Opiniones.ETL.Worker;

public sealed class ExtractionWorker : BackgroundService
{
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly IStagingWriter _staging;
    private readonly IDimensionLoader _dimensionLoader;
    private readonly WarehouseOptions _warehouseOptions;
    private readonly DatabaseDemoInitializer _demo;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(IEnumerable<IExtractor> extractores, IStagingWriter staging,
        IDimensionLoader dimensionLoader, IOptions<WarehouseOptions> warehouseOptions,
        DatabaseDemoInitializer demo, IHostApplicationLifetime lifetime,
        ILogger<ExtractionWorker> logger)
    {
        _extractores = extractores;
        _staging = staging;
        _dimensionLoader = dimensionLoader;
        _warehouseOptions = warehouseOptions.Value;
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

            // Un archivo staging independiente por origen permite extraer en paralelo.
            var resultados = await Task.WhenAll(activos.Select(async extractor =>
            {
                var reloj = Stopwatch.StartNew();
                var resultado = await _staging.GuardarAsync(extractor, stoppingToken);
                _logger.LogInformation("Fuente {Fuente}: {Registros} registros en {Tiempo} ms. Staging: {Ruta}",
                    extractor.Nombre, resultado.Registros, reloj.ElapsedMilliseconds, resultado.Ruta);
                return resultado;
            }));

            _logger.LogInformation("Extracción FINALIZADA: {Registros} registros en {Tiempo} ms.",
                resultados.Sum(x => x.Registros), total.ElapsedMilliseconds);

            if (_warehouseOptions.Enabled)
            {
                var relojCarga = Stopwatch.StartNew();
                var resumen = await _dimensionLoader.CargarAsync(
                    resultados.Select(x => x.Ruta).ToArray(), stoppingToken);
                _logger.LogInformation("DIMENSIONES CARGADAS: {Procesadas} opiniones analizadas en {Tiempo} ms.",
                    resumen.OpinionesProcesadas, relojCarga.ElapsedMilliseconds);
                _logger.LogInformation("DimProducto={Producto} | DimCliente={Cliente} | DimFecha={Fecha} | DimFuente={Fuente} | DimSentimiento={Sentimiento}",
                    resumen.Productos, resumen.Clientes, resumen.Fechas,
                    resumen.Fuentes, resumen.Sentimientos);
                _logger.LogInformation("DATA WAREHOUSE: {Ruta}", resumen.RutaBaseDatos);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Proceso cancelado.");
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            _logger.LogError(ex, "Error en extracción o carga de dimensiones. No se reporta éxito.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
