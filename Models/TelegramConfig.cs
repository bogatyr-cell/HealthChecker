namespace HealthChecker.Models;

public sealed class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;

    public string ChatId { get; set; } = string.Empty;
}
