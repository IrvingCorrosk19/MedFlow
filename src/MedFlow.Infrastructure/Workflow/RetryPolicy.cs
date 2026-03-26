using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedFlow.Infrastructure.Workflow;

public sealed class RetryPolicy
{
    [JsonPropertyName("maxAttempts")]
    public int MaxAttempts { get; set; } = 3;

    [JsonPropertyName("delaySeconds")]
    public int DelaySeconds { get; set; } = 60;

    [JsonPropertyName("backoffType")]
    public string BackoffType { get; set; } = "simple"; // simple | exponential

    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; } = 2.0;

    public static RetryPolicy Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new RetryPolicy();

        try
        {
            return JsonSerializer.Deserialize<RetryPolicy>(json) ?? new RetryPolicy();
        }
        catch
        {
            return new RetryPolicy();
        }
    }

    public int GetDelaySeconds(int attemptNumber)
    {
        if (BackoffType.Equals("exponential", StringComparison.OrdinalIgnoreCase))
            return (int)(DelaySeconds * Math.Pow(Multiplier, attemptNumber - 1));
        return DelaySeconds;
    }
}
