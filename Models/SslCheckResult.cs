namespace HealthChecker.Models;

public sealed class SslCheckResult
{
    public bool IsValid { get; set; }

    public bool IsExpiringSoon { get; set; }

    public int? DaysRemaining { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? ErrorMessage { get; set; }
}
