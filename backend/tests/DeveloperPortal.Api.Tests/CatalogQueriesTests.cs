using DeveloperPortal.Api.Data;
using DeveloperPortal.Api.Models;

namespace DeveloperPortal.Api.Tests;

public class CatalogQueriesTests
{
    [Fact]
    public void ListApis_sin_filtros_no_tiene_parametros()
    {
        var q = CatalogQueries.ListApis(CatalogFilter.Parse(null, null, null));
        Assert.Empty(q.Parameters);
        Assert.DoesNotContain("IN (", q.Sql);
    }

    [Fact]
    public void ListApis_filtra_por_varias_tecnologias_con_parametros()
    {
        var q = CatalogQueries.ListApis(CatalogFilter.Parse("dotnet, MuleSoft,dotnet", null, null));
        Assert.Contains("a.TECHNOLOGY_SLUG IN (?, ?)", q.Sql);
        Assert.Equal(new object?[] { "dotnet", "mulesoft" }, q.Parameters);
    }

    [Fact]
    public void ListApis_busqueda_escapa_comodines_y_no_concatena_input()
    {
        var q = CatalogQueries.ListApis(CatalogFilter.Parse(null, "100%_'; DROP TABLE APIS;--", null));
        Assert.DoesNotContain("DROP TABLE", q.Sql);
        Assert.Equal(4, q.Parameters.Count);
        Assert.All(q.Parameters, p => Assert.Equal(@"%100\%\_'; DROP TABLE APIS;--%", p));
    }

    [Fact]
    public void Placeholders_coinciden_con_parametros()
    {
        var q = CatalogQueries.ListApis(CatalogFilter.Parse("dotnet,java", "pagos", "Pagos"));
        Assert.Equal(q.Sql.Count(c => c == '?'), q.Parameters.Count);

        var upsert = CatalogQueries.UpsertApi(new ApiUpsertRequest { Slug = "x" }, "[]", "user");
        Assert.Equal(upsert.Sql.Count(c => c == '?'), upsert.Parameters.Count);

        var ep = CatalogQueries.InsertEndpoint("x", new ApiEndpoint("1", "GET", "/a", "t", "", null, null), 0);
        Assert.Equal(ep.Sql.Count(c => c == '?'), ep.Parameters.Count);
    }

    [Fact]
    public void Busqueda_se_trunca_a_100_caracteres()
    {
        var filter = CatalogFilter.Parse(null, new string('a', 500), null);
        Assert.Equal(100, filter.Query!.Length);
    }

    [Fact]
    public void ConnectionString_usa_key_pair_y_escapa_signos_igual()
    {
        var cs = SnowflakeConnectionFactory.BuildConnectionString(new SnowflakeSecret
        {
            Account = "acme-xy12345", User = "SVC_PORTAL", PrivateKey = "-----BEGIN PRIVATE KEY-----\nAB==\n-----END PRIVATE KEY-----",
            Warehouse = "WH", Database = "DB", Schema = "SC", Role = "R",
        });
        Assert.Contains("authenticator=snowflake_jwt;", cs);
        Assert.Contains("AB====", cs);
        Assert.DoesNotContain("password", cs);
    }
}
