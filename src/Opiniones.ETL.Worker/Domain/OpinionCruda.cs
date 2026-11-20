namespace Opiniones.ETL.Worker.Domain;

// Esquema común de extracción: aún NO se transforman ni clasifican sentimientos.
public sealed record OpinionCruda(
    string Fuente,
    string IdOrigen,
    string ProductoId,
    string ClienteId,
    string Comentario,
    int? Calificacion,
    DateTime FechaUtc);
