using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace DeveloperPortal.Api.Auth;

public sealed class AzureAdOptions
{
    public const string Section = "AzureAd";

    public string TenantId { get; set; } = "";
    /// <summary>Client ID de la App Registration (audiencia del access token v2).</summary>
    public string ClientId { get; set; } = "";
    public string AdminRole { get; set; } = "Portal.Admin";
    /// <summary>Solo desarrollo local: autentica un usuario administrador ficticio.</summary>
    public bool UseDevelopmentAuth { get; set; }
}

public static class AuthSetup
{
    public const string AdminPolicy = "PortalAdmin";

    /// <summary>
    /// API Gateway ya valida el JWT (authorizer); aquí se valida de nuevo (defensa en profundidad)
    /// para obtener los app roles de Azure AD y aplicar autorización por rol.
    /// </summary>
    public static IServiceCollection AddPortalAuth(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var azure = config.GetSection(AzureAdOptions.Section).Get<AzureAdOptions>() ?? new AzureAdOptions();

        if (azure.UseDevelopmentAuth && env.IsDevelopment())
        {
            services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(DevelopmentAuthHandler.SchemeName, _ => { });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(azure.TenantId) || string.IsNullOrWhiteSpace(azure.ClientId))
                throw new InvalidOperationException("AzureAd:TenantId y AzureAd:ClientId son obligatorios.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.Authority = $"https://login.microsoftonline.com/{azure.TenantId}/v2.0";
                    o.MapInboundClaims = false;
                    o.TokenValidationParameters.ValidIssuer = $"https://login.microsoftonline.com/{azure.TenantId}/v2.0";
                    o.TokenValidationParameters.ValidAudiences = [azure.ClientId, $"api://{azure.ClientId}"];
                    o.TokenValidationParameters.RoleClaimType = "roles";
                    o.TokenValidationParameters.NameClaimType = "preferred_username";
                });
        }

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AdminPolicy, p => p.RequireAuthenticatedUser().RequireRole(azure.AdminRole));

        return services;
    }

    public static string UserIdentifier(this ClaimsPrincipal user) =>
        user.FindFirstValue("preferred_username") ?? user.FindFirstValue("oid") ?? user.Identity?.Name ?? "desconocido";
}

internal sealed class DevelopmentAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = config[$"{AzureAdOptions.Section}:AdminRole"] ?? "Portal.Admin";
        var identity = new ClaimsIdentity(
            [new Claim("preferred_username", "dev@localhost"), new Claim("roles", role)],
            SchemeName, "preferred_username", "roles");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
