using Microsoft.Extensions.Logging;
using HealthChecker.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HealthChecker.Utils;

public sealed class ConfigLoader : IConfigLoader
{
    private readonly ILogger<ConfigLoader> _logger;

    public ConfigLoader(ILogger<ConfigLoader> logger)
    {
        _logger = logger;
    }

    public AppConfig Load(string path = "config.yaml")
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file '{path}' was not found.");
        }

        string yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        AppConfig config = deserializer.Deserialize<AppConfig>(yaml) ?? new AppConfig();
        ApplyEnvironmentOverrides(config);
        ValidateAndNormalize(config);

        _logger.LogInformation(
            "Configuration loaded. Targets: {TargetCount}, interval: {Interval}s, timeout: {Timeout}s",
            config.Targets.Count,
            config.CheckIntervalSeconds,
            config.TimeoutSeconds);

        return config;
    }

    private static void ApplyEnvironmentOverrides(AppConfig config)
    {
        string? token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        string? chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");

        if (!string.IsNullOrWhiteSpace(token))
        {
            config.Telegram.BotToken = token;
        }

        if (!string.IsNullOrWhiteSpace(chatId))
        {
            config.Telegram.ChatId = chatId;
        }
    }

    private static void ValidateAndNormalize(AppConfig config)
    {
        if (config.CheckIntervalSeconds <= 0)
        {
            config.CheckIntervalSeconds = 60;
        }

        if (config.TimeoutSeconds <= 0)
        {
            config.TimeoutSeconds = 5;
        }

        if (config.SslExpireDays <= 0)
        {
            config.SslExpireDays = 7;
        }

        if (config.ResponseTimeWarningMs <= 0)
        {
            config.ResponseTimeWarningMs = 2000;
        }

        foreach (TargetConfig target in config.Targets)
        {
            if (target.SuccessCodes.Count == 0)
            {
                target.SuccessCodes.Add(200);
            }
        }
    }
}
