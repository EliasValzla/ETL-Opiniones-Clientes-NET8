var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// API REST LOCAL, con comentarios inventados, exclusivamente para la demostración.
var comentarios = new[]
{
    new Comentario("A001", "P001", "C009", "El producto cumplió mis expectativas", 5,
        new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc)),
    new Comentario("A002", "P002", "C010", "La entrega fue lenta", 2,
        new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)),
    new Comentario("A003", "P003", "C011", "Podría mejorar el empaque", 3,
        new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc))
};

app.MapGet("/api/comentarios", () => Results.Ok(comentarios));
app.MapGet("/", () => Results.Ok("API de demostración de Opiniones - /api/comentarios"));
app.Urls.Add("http://127.0.0.1:5187");
app.Run();

public sealed record Comentario(string Id, string ProductoId, string ClienteId,
    string Texto, int? Calificacion, DateTime FechaUtc);
