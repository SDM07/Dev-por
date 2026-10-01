using System.Text.Json;

namespace DeveloperPortal.Api.Observability;

public interface IMetrics
{
    void QueryDuration(string operation, TimeSpan elapsed);
    void Count(string metric, string operation, double value = 1);
}

/// <summary>
/// Publica métricas personalizadas en CloudWatch usando Embedded Metric Format (EMF):
/// basta con escribir el JSON en stdout y CloudWatch Logs las extrae, sin llamadas extra a la API.
/// </summary>
public sealed class EmfMetrics : IMetrics
{
    public const string Namespace = "DeveloperPortal";

    public void QueryDuration(string operation, TimeSpan elapsed) =>
        Emit("SnowflakeQueryDuration", "Milliseconds", operation, elapsed.TotalMilliseconds);

    public void Count(string metric, string operation, double value = 1) =>
        Emit(metric, "Count", operation, value);

    private static void Emit(string metric, string unit, string operation, double value)
    {
        var payload = new Dictionary<string, object>
        {
            ["_aws"] = new
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CloudWatchMetrics = new[]
                {
                    new
                    {
                        Namespace,
                        Dimensions = new[] { new[] { "Operation" } },
                        Metrics = new[] { new { Name = metric, Unit = unit } },
                    },
                },
            },
            ["Operation"] = operation,
            [metric] = value,
        };
        Console.WriteLine(JsonSerializer.Serialize(payload));
    }
}
