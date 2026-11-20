using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Domain;

namespace Opiniones.ETL.Worker.Infrastructure;

public sealed class ApiExtractor : IExtractor
{
    private readonly ApiOptions _op;
    private readonly IHttpClientFactory _factory;

    public ApiExtractor(IOptions<ApiOptions> options, IHttpClientFactory factory)
    {
        _op = options.Value;
        _factory = factory;
    }

    public string Nombre => "comentarios_api";
    public bool Habilitado => _op.Enabled;

    public async IAsyncEnumerable<OpinionCruda> ExtraerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = _factory.CreateClient("OpinionesApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, _op.Endpoint);
        if (!string.IsNullOrWhiteSpace(_op.ApiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", _op.ApiKey);

        using var response = await client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        await foreach (var comentario in JsonSerializer.DeserializeAsyncEnumerable<ComentarioApi>(
            stream, jsonOptions, cancellationToken: cancellationToken))
        {
            if (comentario is null) continue;
            if (string.IsNullOrWhiteSpace(comentario.Id) || string.IsNullOrWhiteSpace(comentario.Texto))
                throw new InvalidDataException("API: comentario sin Id o texto.");

            yield return new OpinionCruda("api_rest", comentario.Id,
                comentario.ProductoId, comentario.ClienteId, comentario.Texto,
                comentario.Calificacion, comentario.FechaUtc.ToUniversalTime());
        }
    }

    private sealed class ComentarioApi
    {
        public string Id { get; set; } = "";
        public string ProductoId { get; set; } = "";
        public string ClienteId { get; set; } = "";
        public string Texto { get; set; } = "";
        public int? Calificacion { get; set; }
        public DateTime FechaUtc { get; set; }
    }
}
