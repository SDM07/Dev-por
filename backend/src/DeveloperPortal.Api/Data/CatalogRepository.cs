using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using DeveloperPortal.Api.Models;
using DeveloperPortal.Api.Observability;
using Microsoft.Extensions.Options;

namespace DeveloperPortal.Api.Data;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Technology>> ListTechnologiesAsync(CancellationToken ct);
    Task<IReadOnlyList<ApiSummary>> ListApisAsync(CatalogFilter filter, CancellationToken ct);
    Task<ApiDetail?> GetApiAsync(string slug, CancellationToken ct);
    Task<bool> ApiExistsAsync(string slug, CancellationToken ct);
    Task<bool> TechnologyExistsAsync(string slug, CancellationToken ct);
    Task SaveApiAsync(ApiUpsertRequest api, string updatedBy, CancellationToken ct);
    Task<bool> DeleteApiAsync(string slug, CancellationToken ct);
}

public sealed class SnowflakeCatalogRepository(
    ISnowflakeConnectionFactory factory,
    IOptions<SnowflakeOptions> options,
    IMetrics metrics) : ICatalogRepository
{
    private int Timeout => options.Value.CommandTimeoutSeconds;

    public Task<IReadOnlyList<Technology>> ListTechnologiesAsync(CancellationToken ct) =>
        QueryAsync("ListTechnologies", CatalogQueries.ListTechnologies(), r => new Technology(
            r.GetString(0), r.GetString(1), r.GetString(2), Convert.ToInt32(r.GetValue(3))), ct);

    public Task<IReadOnlyList<ApiSummary>> ListApisAsync(CatalogFilter filter, CancellationToken ct) =>
        QueryAsync("ListApis", CatalogQueries.ListApis(filter), ReadSummary, ct);

    public async Task<ApiDetail?> GetApiAsync(string slug, CancellationToken ct)
    {
        await using var conn = await factory.OpenAsync(ct);
        var sw = Stopwatch.StartNew();

        ApiDetail? detail = null;
        await using (var cmd = CatalogQueries.GetApi(slug).CreateCommand(conn, Timeout))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (await r.ReadAsync(ct))
            {
                var s = ReadSummary(r);
                detail = new ApiDetail(s.Slug, s.Name, s.Summary, s.Technology, s.TechnologyName, s.Category,
                    s.Version, s.OwnerTeam, s.Status, s.Tags, s.UpdatedAt,
                    DescriptionMd: GetStringOrEmpty(r, 11), BaseUrl: GetStringOrEmpty(r, 12), Endpoints: []);
            }
        }

        if (detail is null) return null;

        var endpoints = new List<ApiEndpoint>();
        await using (var cmd = CatalogQueries.GetEndpoints(slug).CreateCommand(conn, Timeout))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                endpoints.Add(new ApiEndpoint(
                    r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), GetStringOrEmpty(r, 4),
                    r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6)));
            }
        }

        metrics.QueryDuration("GetApi", sw.Elapsed);
        return detail with { Endpoints = endpoints };
    }

    public Task<bool> ApiExistsAsync(string slug, CancellationToken ct) =>
        ScalarExistsAsync(CatalogQueries.ApiExists(slug), ct);

    public Task<bool> TechnologyExistsAsync(string slug, CancellationToken ct) =>
        ScalarExistsAsync(CatalogQueries.TechnologyExists(slug), ct);

    public async Task SaveApiAsync(ApiUpsertRequest api, string updatedBy, CancellationToken ct)
    {
        await using var conn = await factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        var sw = Stopwatch.StartNew();

        await ExecuteAsync(CatalogQueries.UpsertApi(api, JsonSerializer.Serialize(api.Tags), updatedBy), conn, tx, ct);
        await ExecuteAsync(CatalogQueries.DeleteEndpoints(api.Slug), conn, tx, ct);
        for (var i = 0; i < api.Endpoints.Count; i++)
        {
            await ExecuteAsync(CatalogQueries.InsertEndpoint(api.Slug, api.Endpoints[i], i), conn, tx, ct);
        }

        await tx.CommitAsync(ct);
        metrics.QueryDuration("SaveApi", sw.Elapsed);
    }

    public async Task<bool> DeleteApiAsync(string slug, CancellationToken ct)
    {
        await using var conn = await factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await ExecuteAsync(CatalogQueries.DeleteEndpoints(slug), conn, tx, ct);
        var deleted = await ExecuteAsync(CatalogQueries.DeleteApi(slug), conn, tx, ct);
        await tx.CommitAsync(ct);
        return deleted > 0;
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string name, SqlQuery query, Func<DbDataReader, T> map, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        await using var conn = await factory.OpenAsync(ct);
        await using var cmd = query.CreateCommand(conn, Timeout);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var results = new List<T>();
        while (await r.ReadAsync(ct)) results.Add(map(r));
        metrics.QueryDuration(name, sw.Elapsed);
        return results;
    }

    private async Task<bool> ScalarExistsAsync(SqlQuery query, CancellationToken ct)
    {
        await using var conn = await factory.OpenAsync(ct);
        await using var cmd = query.CreateCommand(conn, Timeout);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)) > 0;
    }

    private async Task<int> ExecuteAsync(SqlQuery query, DbConnection conn, DbTransaction tx, CancellationToken ct)
    {
        await using var cmd = query.CreateCommand(conn, Timeout, tx);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static ApiSummary ReadSummary(DbDataReader r) => new(
        Slug: r.GetString(0),
        Name: r.GetString(1),
        Summary: GetStringOrEmpty(r, 2),
        Technology: r.GetString(3),
        TechnologyName: r.GetString(4),
        Category: GetStringOrEmpty(r, 5),
        Version: GetStringOrEmpty(r, 6),
        OwnerTeam: GetStringOrEmpty(r, 7),
        Status: Enum.TryParse<ApiStatus>(r.GetString(8), ignoreCase: true, out var st) ? st : ApiStatus.STABLE,
        Tags: r.IsDBNull(9) ? [] : JsonSerializer.Deserialize<List<string>>(r.GetString(9)) ?? [],
        UpdatedAt: ReadTimestamp(r.GetValue(10)));

    private static string GetStringOrEmpty(DbDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);

    private static DateTimeOffset ReadTimestamp(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        _ => DateTimeOffset.Parse(value.ToString()!),
    };
}
