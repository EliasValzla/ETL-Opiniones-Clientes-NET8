namespace Opiniones.ETL.Worker.Domain;

public sealed record DimensionLoadResult(
    int OpinionesProcesadas,
    int Productos,
    int Clientes,
    int Fechas,
    int Fuentes,
    int Sentimientos,
    string RutaBaseDatos);
