using DeveloperPortal.Api.Auth;
using DeveloperPortal.Api.Data;
using DeveloperPortal.Api.Models;
using DeveloperPortal.Api.Observability;
using DeveloperPortal.Api.Validation;

namespace DeveloperPortal.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        api.MapGet("/technologies", async (ICatalogRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.ListTechnologiesAsync(ct)));

        // GET /api/apis?technology=dotnet,mulesoft&q=pagos&category=Pagos
        api.MapGet("/apis", async (string? technology, string? q, string? category, ICatalogRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.ListApisAsync(CatalogFilter.Parse(technology, q, category), ct)));

        api.MapGet("/apis/{slug}", async (string slug, ICatalogRepository repo, CancellationToken ct) =>
            await repo.GetApiAsync(slug, ct) is { } detail ? Results.Ok(detail) : Results.NotFound());

        var admin = api.MapGroup("/apis").RequireAuthorization(AuthSetup.AdminPolicy);

        admin.MapPost("", async (ApiUpsertRequest body, HttpContext http, ICatalogRepository repo,
            IMetrics metrics, ILogger<ApiUpsertRequest> log, CancellationToken ct) =>
        {
            if (await ValidateAsync(body, repo, ct) is { } invalid) return invalid;
            if (await repo.ApiExistsAsync(body.Slug, ct))
                return Results.Problem($"Ya existe una API con el slug '{body.Slug}'.", statusCode: StatusCodes.Status409Conflict);

            await repo.SaveApiAsync(body, http.User.UserIdentifier(), ct);
            log.LogInformation("API {Slug} creada por {User}", body.Slug, http.User.UserIdentifier());
            metrics.Count("CatalogChanges", "Create");
            return Results.Created($"/api/apis/{body.Slug}", await repo.GetApiAsync(body.Slug, ct));
        });

        admin.MapPut("/{slug}", async (string slug, ApiUpsertRequest body, HttpContext http, ICatalogRepository repo,
            IMetrics metrics, ILogger<ApiUpsertRequest> log, CancellationToken ct) =>
        {
            body.Slug = slug;
            if (await ValidateAsync(body, repo, ct) is { } invalid) return invalid;
            if (!await repo.ApiExistsAsync(slug, ct)) return Results.NotFound();

            await repo.SaveApiAsync(body, http.User.UserIdentifier(), ct);
            log.LogInformation("API {Slug} actualizada por {User}", slug, http.User.UserIdentifier());
            metrics.Count("CatalogChanges", "Update");
            return Results.Ok(await repo.GetApiAsync(slug, ct));
        });

        admin.MapDelete("/{slug}", async (string slug, HttpContext http, ICatalogRepository repo,
            IMetrics metrics, ILogger<ApiUpsertRequest> log, CancellationToken ct) =>
        {
            if (!await repo.DeleteApiAsync(slug, ct)) return Results.NotFound();
            log.LogInformation("API {Slug} eliminada por {User}", slug, http.User.UserIdentifier());
            metrics.Count("CatalogChanges", "Delete");
            return Results.NoContent();
        });

        return app;
    }

    private static async Task<IResult?> ValidateAsync(ApiUpsertRequest body, ICatalogRepository repo, CancellationToken ct)
    {
        var errors = ApiUpsertValidator.Validate(body);
        if (errors.Count == 0 && !await repo.TechnologyExistsAsync(body.Technology, ct))
            errors["technology"] = ["La tecnología no existe en el catálogo."];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }
}
