using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Application;

public interface IExtractor
{
    string Nombre { get; }
    bool Habilitado { get; }
    IAsyncEnumerable<OpinionCruda> ExtraerAsync(CancellationToken cancellationToken);
}
