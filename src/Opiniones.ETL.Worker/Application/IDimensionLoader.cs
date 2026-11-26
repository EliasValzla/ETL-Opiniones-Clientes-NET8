using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Application;

public interface IDimensionLoader
{
    Task<DimensionLoadResult> CargarAsync(IReadOnlyCollection<string> rutasStaging,
        CancellationToken cancellationToken);
}
