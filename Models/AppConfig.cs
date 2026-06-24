namespace HealthChecker.Models;

public sealed class AppConfig
{
    public int CheckIntervalSeconds { get; set; } = 60;

    public int TimeoutSeconds { get; set; } = 5;

    public int SslExpireDays { get; set; } = 7;

    public int ResponseTimeWarningMs { get; set; } = 2000;

    public TelegramConfig Telegram { get; set; } = new();

    public List<TargetConfig> Targets { get; set; } = new();
}
