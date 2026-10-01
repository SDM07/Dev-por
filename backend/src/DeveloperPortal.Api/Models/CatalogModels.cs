using System.Text.Json.Serialization;

namespace DeveloperPortal.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ApiStatus>))]
public enum ApiStatus { STABLE, BETA, DEPRECATED }

public sealed record Technology(string Slug, string Name, string Color, int ApiCount);

public record ApiSummary(
    string Slug,
    string Name,
    string Summary,
    string Technology,
    string TechnologyName,
    string Category,
    string Version,
    string OwnerTeam,
    ApiStatus Status,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt);

public sealed record ApiEndpoint(
    string Id,
    string Method,
    string Path,
    string Title,
    string DescriptionMd,
    string? RequestExample,
    string? ResponseExample);

public sealed record ApiDetail(
    string Slug,
    string Name,
    string Summary,
    string Technology,
    string TechnologyName,
    string Category,
    string Version,
    string OwnerTeam,
    ApiStatus Status,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt,
    string DescriptionMd,
    string BaseUrl,
    IReadOnlyList<ApiEndpoint> Endpoints);

/// <summary>Payload de creación/edición enviado por el panel de administración.</summary>
public sealed class ApiUpsertRequest
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Technology { get; set; } = "";
    public string Category { get; set; } = "";
    public string Version { get; set; } = "";
    public string OwnerTeam { get; set; } = "";
    public ApiStatus Status { get; set; } = ApiStatus.STABLE;
    public List<string> Tags { get; set; } = [];
    public string BaseUrl { get; set; } = "";
    public string DescriptionMd { get; set; } = "";
    public List<ApiEndpoint> Endpoints { get; set; } = [];
}

public sealed record CatalogFilter(IReadOnlyList<string> Technologies, string? Query, string? Category)
{
    public static CatalogFilter Parse(string? technology, string? q, string? category) => new(
        (technology ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(20)
            .ToList(),
        string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)],
        string.IsNullOrWhiteSpace(category) ? null : category.Trim());
}
