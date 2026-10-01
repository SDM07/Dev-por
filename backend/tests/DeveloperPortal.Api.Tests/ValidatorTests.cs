using DeveloperPortal.Api.Models;
using DeveloperPortal.Api.Validation;

namespace DeveloperPortal.Api.Tests;

public class ValidatorTests
{
    internal static ApiUpsertRequest Valid() => new()
    {
        Slug = "accounts",
        Name = "Cuentas",
        Summary = "Resumen",
        Technology = "dotnet",
        Category = "Core",
        Version = "v1",
        OwnerTeam = "Core",
        BaseUrl = "https://api.empresa.com/accounts",
        Endpoints = [new ApiEndpoint("e1", "GET", "/accounts", "Listar", "", null, null)],
    };

    [Fact]
    public void Payload_valido_no_tiene_errores() => Assert.Empty(ApiUpsertValidator.Validate(Valid()));

    [Theory]
    [InlineData("Mayusculas")]
    [InlineData("con espacio")]
    [InlineData("-inicio")]
    [InlineData("")]
    public void Slug_invalido(string slug)
    {
        var api = Valid();
        api.Slug = slug;
        Assert.Contains("slug", ApiUpsertValidator.Validate(api).Keys);
    }

    [Fact]
    public void BaseUrl_debe_ser_http()
    {
        var api = Valid();
        api.BaseUrl = "javascript:alert(1)";
        Assert.Contains("baseUrl", ApiUpsertValidator.Validate(api).Keys);
    }

    [Fact]
    public void Endpoint_con_metodo_y_path_invalidos()
    {
        var api = Valid();
        api.Endpoints = [new ApiEndpoint("e1", "TRACE", "sin-slash", "", "", null, null)];
        var errors = ApiUpsertValidator.Validate(api);
        Assert.Contains("endpoints[0].method", errors.Keys);
        Assert.Contains("endpoints[0].path", errors.Keys);
        Assert.Contains("endpoints[0].title", errors.Keys);
    }
}
