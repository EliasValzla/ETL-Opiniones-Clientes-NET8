namespace Opiniones.ETL.Worker.Application;

public interface IStagingWriter
{
    Task<(int Registros, string Ruta)> GuardarAsync(IExtractor extractor, CancellationToken ct);
}
