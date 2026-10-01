namespace DeveloperPortal.Api.Data;

public sealed class SnowflakeOptions
{
    public const string Section = "Snowflake";

    /// <summary>ARN o nombre del secreto en AWS Secrets Manager con las credenciales key-pair.</summary>
    public string SecretId { get; set; } = "";

    /// <summary>Solo para desarrollo local: connection string completa (omite Secrets Manager).</summary>
    public string? ConnectionString { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>Estructura JSON del secreto almacenado en Secrets Manager.</summary>
public sealed class SnowflakeSecret
{
    public string Account { get; set; } = "";
    public string User { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string? PrivateKeyPassphrase { get; set; }
    public string Warehouse { get; set; } = "";
    public string Database { get; set; } = "";
    public string Schema { get; set; } = "";
    public string Role { get; set; } = "";
}
