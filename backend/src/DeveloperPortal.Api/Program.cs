using System.Text.Json.Serialization;
using Amazon.SecretsManager;
using DeveloperPortal.Api.Auth;
using DeveloperPortal.Api.Data;
using DeveloperPortal.Api.Endpoints;
using DeveloperPortal.Api.Observability;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Ejecuta como Lambda detrás de API Gateway HTTP API; en local se comporta como Kestrel normal.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// Logs JSON de una línea: CloudWatch Logs Insights puede consultarlos por campo.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
    o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    o.UseUtcTimestamp = true;
});

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddPortalAuth(builder.Configuration, builder.Environment);

builder.Services.Configure<SnowflakeOptions>(builder.Configuration.GetSection(SnowflakeOptions.Section));
builder.Services.AddSingleton<IAmazonSecretsManager, AmazonSecretsManagerClient>();
builder.Services.AddSingleton<ISnowflakeConnectionFactory, SnowflakeConnectionFactory>();
builder.Services.AddSingleton<IMetrics, EmfMetrics>();
builder.Services.AddScoped<ICatalogRepository, SnowflakeCatalogRepository>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(ex, "Error no controlado en {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
    ctx.RequestServices.GetRequiredService<IMetrics>().Count("UnhandledErrors", ctx.Request.Path.Value ?? "unknown");
    await Results.Problem("Ocurrió un error inesperado.", statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(ctx);
}));

app.UseMiddleware<OriginVerifyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapCatalogEndpoints();

app.Run();

public partial class Program;
