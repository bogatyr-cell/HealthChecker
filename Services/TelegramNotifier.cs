using System;
using HealthChecker.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HealthChecker.Services;

public sealed class TelegramNotifier
{
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(ILogger<TelegramNotifier> logger)
    {
        _logger = logger;
    }

    public async Task SendAlertAsync(
        TelegramConfig telegramConfig,
        CheckResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telegramConfig.BotToken)
            || string.IsNullOrWhiteSpace(telegramConfig.ChatId))
        {
            _logger.LogWarning("Telegram is not configured. Alert was not sent.");
            return;
        }

        try
        {
            var botClient = new TelegramBotClient(telegramConfig.BotToken);

            ChatId chatId = long.TryParse(telegramConfig.ChatId, out long numericChatId)
                ? new ChatId(numericChatId)
                : new ChatId(telegramConfig.ChatId);

            string message = BuildMessage(result);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Telegram alert sent for {Name}", result.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram alert for {Name}", result.Name);
        }
    }

    private static string BuildMessage(CheckResult result)
    {
        string status = result.StatusCode.HasValue
            ? result.StatusCode.Value.ToString()
            : "нет ответа";

        string sslDays = result.SslDaysRemaining.HasValue
            ? result.SslDaysRemaining.Value.ToString()
            : "не проверено";

        string problem = TranslateProblem(result.ErrorMessage);

        string message =
            $"⚠️ Предупреждение HealthChecker{Environment.NewLine}{Environment.NewLine}" +
            $"Сервис: {result.Name}{Environment.NewLine}" +
            $"URL: {result.Url}{Environment.NewLine}" +
            $"Статус: {status}{Environment.NewLine}" +
            $"Время ответа: {result.ResponseTimeMs} мс{Environment.NewLine}" +
            $"SSL осталось дней: {sslDays}{Environment.NewLine}" +
            $"Проблема: {problem}{Environment.NewLine}" +
            $"Проверено UTC: {result.CheckedAt:yyyy-MM-dd HH:mm:ss}";

        return message;
    }

    private static string TranslateProblem(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Неизвестная ошибка.";
        }

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("TaskCanceledException", StringComparison.OrdinalIgnoreCase))
        {
            return "Таймаут запроса: сервис не ответил за заданное время.";
        }

        if (errorMessage.Contains("unexpected status", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("status code", StringComparison.OrdinalIgnoreCase))
        {
            return "Сервис вернул неожиданный HTTP-статус.";
        }

        if (errorMessage.Contains("EOF", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("transport stream", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            return "Ошибка проверки SSL-сертификата или защищённого соединения.";
        }

        if (errorMessage.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("No such host", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("nodename", StringComparison.OrdinalIgnoreCase))
        {
            return "Сайт недоступен: не удалось определить адрес хоста.";
        }

        return errorMessage;
    }
}
