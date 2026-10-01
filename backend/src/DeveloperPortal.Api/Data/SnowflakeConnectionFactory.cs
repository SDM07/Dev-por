using System.Data.Common;
using System.Text;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Options;
using Snowflake.Data.Client;

namespace DeveloperPortal.Api.Data;

public interface ISnowflakeConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken ct);
}

/// <summary>
/// Construye la conexión a Snowflake con autenticación key-pair (snowflake_jwt).
/// El secreto se lee una sola vez por contenedor Lambda y se reutiliza entre invocaciones.
/// </summary>
public sealed class SnowflakeConnectionFactory(
    IOptions<SnowflakeOptions> options,
    IAmazonSecretsManager secretsManager,
    ILogger<SnowflakeConnectionFactory> logger) : ISnowflakeConnectionFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _connectionString;

    public async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SnowflakeDbConnection { ConnectionString = await GetConnectionStringAsync(ct) };
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        if (_connectionString is not null) return _connectionString;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connectionString is not null) return _connectionString;

            var opts = options.Value;
            if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
            {
                _connectionString = opts.ConnectionString;
                return _connectionString;
            }

            logger.LogInformation("Leyendo credenciales de Snowflake desde Secrets Manager");
            var response = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest { SecretId = opts.SecretId }, ct);
            var secret = JsonSerializer.Deserialize<SnowflakeSecret>(response.SecretString, JsonOptions)
                         ?? throw new InvalidOperationException("El secreto de Snowflake está vacío");

            _connectionString = BuildConnectionString(secret);
            return _connectionString;
        }
        finally
        {
            _lock.Release();
        }
    }

    internal static string BuildConnectionString(SnowflakeSecret s)
    {
        // En la connection string de Snowflake.Data el '=' dentro de un valor se escapa duplicándolo.
        static string Escape(string v) => v.Replace("=", "==");

        var sb = new StringBuilder()
            .Append("account=").Append(s.Account).Append(';')
            .Append("user=").Append(s.User).Append(';')
            .Append("authenticator=snowflake_jwt;")
            .Append("private_key=").Append(Escape(s.PrivateKey)).Append(';')
            .Append("warehouse=").Append(s.Warehouse).Append(';')
            .Append("db=").Append(s.Database).Append(';')
            .Append("schema=").Append(s.Schema).Append(';')
            .Append("role=").Append(s.Role).Append(';')
            .Append("application=DeveloperPortal;");

        if (!string.IsNullOrEmpty(s.PrivateKeyPassphrase))
            sb.Append("private_key_pwd=").Append(Escape(s.PrivateKeyPassphrase)).Append(';');

        return sb.ToString();
    }
}
