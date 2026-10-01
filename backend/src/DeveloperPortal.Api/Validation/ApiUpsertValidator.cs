using System.Text.RegularExpressions;
using DeveloperPortal.Api.Models;

namespace DeveloperPortal.Api.Validation;

public static partial class ApiUpsertValidator
{
    private static readonly HashSet<string> Methods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

    public static Dictionary<string, string[]> Validate(ApiUpsertRequest api)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string field, string message) => errors[field] = [.. errors.GetValueOrDefault(field, []), message];

        if (!SlugRegex().IsMatch(api.Slug) || api.Slug.Length > 80) Add("slug", "Solo minúsculas, números y guiones (máx. 80).");
        Required(api.Name, "name", 120, Add);
        Required(api.Summary, "summary", 300, Add);
        Required(api.Technology, "technology", 40, Add);
        Required(api.Category, "category", 80, Add);
        Required(api.Version, "version", 20, Add);
        Required(api.OwnerTeam, "ownerTeam", 120, Add);
        if (api.DescriptionMd.Length > 100_000) Add("descriptionMd", "Máximo 100.000 caracteres.");
        if (!Uri.TryCreate(api.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            Add("baseUrl", "Debe ser una URL http(s) absoluta.");
        if (api.Tags.Count > 20 || api.Tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 40))
            Add("tags", "Máximo 20 etiquetas de hasta 40 caracteres.");
        if (api.Endpoints.Count > 200) Add("endpoints", "Máximo 200 endpoints por API.");

        for (var i = 0; i < api.Endpoints.Count; i++)
        {
            var e = api.Endpoints[i];
            if (!Methods.Contains(e.Method)) Add($"endpoints[{i}].method", "Método HTTP no soportado.");
            if (string.IsNullOrWhiteSpace(e.Path) || !e.Path.StartsWith('/')) Add($"endpoints[{i}].path", "Debe iniciar con '/'.");
            if (string.IsNullOrWhiteSpace(e.Title)) Add($"endpoints[{i}].title", "Requerido.");
            if (string.IsNullOrWhiteSpace(e.Id) || e.Id.Length > 64) Add($"endpoints[{i}].id", "Requerido (máx. 64).");
        }

        if (api.Endpoints.Select(e => e.Id).Distinct().Count() != api.Endpoints.Count)
            Add("endpoints", "Los IDs de endpoint deben ser únicos.");

        return errors;
    }

    private static void Required(string value, string field, int max, Action<string, string> add)
    {
        if (string.IsNullOrWhiteSpace(value)) add(field, "Requerido.");
        else if (value.Length > max) add(field, $"Máximo {max} caracteres.");
    }
}
