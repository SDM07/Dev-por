using System.Security.Cryptography;
using System.Text;

namespace DeveloperPortal.Api.Auth;

/// <summary>
/// Rechaza las peticiones que no llegan a través de CloudFront (y por tanto del WAF).
/// CloudFront agrega el header X-Origin-Verify con un secreto que solo conocen él y la Lambda.
/// Si no hay secreto configurado (desarrollo local) el middleware no hace nada.
/// </summary>
public sealed class OriginVerifyMiddleware(RequestDelegate next, IConfiguration config, ILogger<OriginVerifyMiddleware> logger)
{
    public const string HeaderName = "X-Origin-Verify";
    private readonly byte[]? _secret = string.IsNullOrEmpty(config["OriginVerify:Secret"])
        ? null
        : Encoding.UTF8.GetBytes(config["OriginVerify:Secret"]!);

    public Task InvokeAsync(HttpContext context)
    {
        if (_secret is null) return next(context);

        var provided = Encoding.UTF8.GetBytes(context.Request.Headers[HeaderName].ToString());
        if (CryptographicOperations.FixedTimeEquals(provided, _secret)) return next(context);

        logger.LogWarning("Petición rechazada: no proviene de CloudFront ({Path})", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
