using Microsoft.Extensions.Options;
using Opiniones.ETL.Worker;
using Opiniones.ETL.Worker.Application;
using Opiniones.ETL.Worker.Configuration;
using Opiniones.ETL.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.Configure<CsvOptions>(builder.Configuration.GetSection("Sources:Csv"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Sources:Database"));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Sources:Api"));
builder.Services.Configure<StagingOptions>(builder.Configuration.GetSection("Staging"));

builder.Services.AddHttpClient("OpinionesApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IExtractor, CsvExtractor>();
builder.Services.AddSingleton<IExtractor, DatabaseExtractor>();
builder.Services.AddSingleton<IExtractor, ApiExtractor>();
builder.Services.AddSingleton<IStagingWriter, JsonlStagingWriter>();
builder.Services.AddSingleton<DatabaseDemoInitializer>();
builder.Services.AddHostedService<ExtractionWorker>();

await builder.Build().RunAsync();
