using System.Net;
using System.Net.Http.Json;
using DeveloperPortal.Api.Data;
using DeveloperPortal.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeveloperPortal.Api.Tests;

internal sealed class FakeRepository : ICatalogRepository
{
    public CatalogFilter? LastFilter { get; private set; }
    public List<ApiUpsertRequest> Saved { get; } = [];

    public Task<IReadOnlyList<Technology>> ListTechnologiesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Technology>>([new("dotnet", ".NET", "#512bd4", 1), new("mulesoft", "MuleSoft", "#00a0df", 0)]);

    public Task<IReadOnlyList<ApiSummary>> ListApisAsync(CatalogFilter filter, CancellationToken ct)
    {
        LastFilter = filter;
        return Task.FromResult<IReadOnlyList<ApiSummary>>([]);
    }

    public Task<ApiDetail?> GetApiAsync(string slug, CancellationToken ct) => Task.FromResult(slug == "accounts"
        ? new ApiDetail("accounts", "Cuentas", "", "dotnet", ".NET", "Core", "v1", "Core", ApiStatus.STABLE, [],
            DateTimeOffset.UnixEpoch, "", "https://x", [])
        : null);

    public Task<bool> ApiExistsAsync(string slug, CancellationToken ct) => Task.FromResult(slug == "accounts");
    public Task<bool> TechnologyExistsAsync(string slug, CancellationToken ct) => Task.FromResult(slug is "dotnet" or "mulesoft");

    public Task SaveApiAsync(ApiUpsertRequest api, string updatedBy, CancellationToken ct)
    {
        Saved.Add(api);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteApiAsync(string slug, CancellationToken ct) => Task.FromResult(slug == "accounts");
}

public class EndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly FakeRepository _repo = new();
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("AzureAd:UseDevelopmentAuth", "true");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<ICatalogRepository>();
                s.AddSingleton<ICatalogRepository>(_repo);
            });
        });
    }

    [Fact]
    public async Task Health_es_anonimo()
    {
        var res = await _factory.CreateClient().GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Lista_apis_pasa_filtro_de_tecnologia()
    {
        var res = await _factory.CreateClient().GetAsync("/api/apis?technology=dotnet,mulesoft&q=pagos");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(["dotnet", "mulesoft"], _repo.LastFilter!.Technologies);
        Assert.Equal("pagos", _repo.LastFilter.Query);
    }

    [Fact]
    public async Task Detalle_inexistente_devuelve_404()
    {
        var res = await _factory.CreateClient().GetAsync("/api/apis/no-existe");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Crear_con_slug_duplicado_devuelve_409()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/apis", ValidatorTests.Valid());
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Crear_con_tecnologia_inexistente_devuelve_400()
    {
        var api = ValidatorTests.Valid();
        api.Slug = "nueva";
        api.Technology = "cobol";
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/apis", api);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Empty(_repo.Saved);
    }

    [Fact]
    public async Task Crear_api_valida_devuelve_201()
    {
        var api = ValidatorTests.Valid();
        api.Slug = "nueva";
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/apis", api);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Single(_repo.Saved);
    }
}

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("AzureAd:TenantId", "11111111-1111-1111-1111-111111111111");
            b.UseSetting("AzureAd:ClientId", "22222222-2222-2222-2222-222222222222");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<ICatalogRepository>();
                s.AddSingleton<ICatalogRepository>(new FakeRepository());
            });
        });
    }

    [Theory]
    [InlineData("/api/technologies")]
    [InlineData("/api/apis")]
    public async Task Sin_token_devuelve_401(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Token_invalido_devuelve_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "no-es-un-jwt");
        var res = await client.DeleteAsync("/api/apis/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}

public class OriginVerifyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OriginVerifyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("AzureAd:UseDevelopmentAuth", "true");
            b.UseSetting("OriginVerify:Secret", "s3cr3t");
        });
    }

    [Fact]
    public async Task Sin_header_de_cloudfront_devuelve_403()
    {
        var res = await _factory.CreateClient().GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Con_header_de_cloudfront_pasa()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Origin-Verify", "s3cr3t");
        var res = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
