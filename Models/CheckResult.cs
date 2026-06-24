namespace HealthChecker.Models;

public sealed class CheckResult
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;

    public int? StatusCode { get; set; }

    public long ResponseTimeMs { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsStatusCodeOk { get; set; }

    public bool IsResponseTimeOk { get; set; }

    public bool IsSslOk { get; set; } = true;

    public int? SslDaysRemaining { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsHealthy => IsAvailable && IsStatusCodeOk && IsResponseTimeOk && IsSslOk;
}
