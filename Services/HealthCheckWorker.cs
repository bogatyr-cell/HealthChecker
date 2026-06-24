using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HealthChecker.Models;
using HealthChecker.Utils;

namespace HealthChecker.Services;

public sealed class HealthCheckWorker : BackgroundService
{
    private readonly IConfigLoader _configLoader;
    private readonly EndpointChecker _endpointChecker;
    private readonly TelegramNotifier _telegramNotifier;
    private readonly ILogger<HealthCheckWorker> _logger;

    public HealthCheckWorker(
        IConfigLoader configLoader,
        EndpointChecker endpointChecker,
        TelegramNotifier telegramNotifier,
        ILogger<HealthCheckWorker> logger)
    {
        _configLoader = configLoader;
        _endpointChecker = endpointChecker;
        _telegramNotifier = telegramNotifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppConfig config;

        try
        {
            config = _configLoader.Load();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "HealthChecker cannot start because configuration is invalid.");
            return;
        }

        _logger.LogInformation("HealthChecker started.");

        await RunChecksAsync(config, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(config.CheckIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool nextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!nextTick)
                {
                    break;
                }

                await RunChecksAsync(config, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HealthChecker loop.");
            }
        }

        _logger.LogInformation("HealthChecker stopped.");
    }

    private async Task RunChecksAsync(AppConfig config, CancellationToken cancellationToken)
    {
        List<TargetConfig> targets = config.Targets
            .Where(target => target.Enabled)
            .ToList();

        if (targets.Count == 0)
        {
            _logger.LogWarning("No enabled targets were found in config.yaml.");
            return;
        }

        _logger.LogInformation("Starting health check cycle. Targets: {Count}", targets.Count);

        foreach (TargetConfig target in targets)
        {
            CheckResult result = await _endpointChecker.CheckAsync(target, config, cancellationToken);

            if (result.IsHealthy)
            {
                _logger.LogInformation(
                    "[OK] {Name} | {Url} | status: {StatusCode} | response: {ResponseTime} ms | SSL days: {SslDays}",
                    result.Name,
                    result.Url,
                    result.StatusCode,
                    result.ResponseTimeMs,
                    result.SslDaysRemaining?.ToString() ?? "not checked");
            }
            else
            {
                _logger.LogWarning(
                    "[ALERT] {Name} | {Url} | status: {StatusCode} | response: {ResponseTime} ms | problem: {Problem}",
                    result.Name,
                    result.Url,
                    result.StatusCode?.ToString() ?? "no response",
                    result.ResponseTimeMs,
                    result.ErrorMessage ?? "Unknown problem");

                await _telegramNotifier.SendAlertAsync(config.Telegram, result, cancellationToken);
            }
        }
    }
}
