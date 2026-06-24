using Microsoft.Extensions.Logging;
using System.Diagnostics;
using HealthChecker.Models;

namespace HealthChecker.Services;

public sealed class EndpointChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SslChecker _sslChecker;
    private readonly ILogger<EndpointChecker> _logger;

    public EndpointChecker(
        IHttpClientFactory httpClientFactory,
        SslChecker sslChecker,
        ILogger<EndpointChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sslChecker = sslChecker;
        _logger = logger;
    }

    public async Task<CheckResult> CheckAsync(
        TargetConfig target,
        AppConfig config,
        CancellationToken cancellationToken)
    {
        var result = new CheckResult
        {
            Name = target.Name,
            Url = target.Url,
            CheckedAt = DateTimeOffset.UtcNow,
        };

        if (!Uri.TryCreate(target.Url, UriKind.Absolute, out Uri? uri))
        {
            result.ErrorMessage = "Invalid URL format.";
            result.IsAvailable = false;
            result.IsStatusCodeOk = false;
            result.IsResponseTimeOk = false;
            result.IsSslOk = false;
            return result;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("HealthChecker/1.0");

            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            int statusCode = (int)response.StatusCode;
            int responseLimit = target.MaxResponseTimeMs > 0
                ? target.MaxResponseTimeMs
                : config.ResponseTimeWarningMs;

            result.StatusCode = statusCode;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsAvailable = true;
            result.IsStatusCodeOk = target.SuccessCodes.Contains(statusCode);
            result.IsResponseTimeOk = result.ResponseTimeMs <= responseLimit;

            if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                int port = uri.Port > 0 ? uri.Port : 443;
                SslCheckResult sslResult = await _sslChecker.CheckAsync(
                    uri.Host,
                    port,
                    config.SslExpireDays,
                    cancellationToken);

                result.SslDaysRemaining = sslResult.DaysRemaining;
                result.IsSslOk = sslResult.IsValid && !sslResult.IsExpiringSoon;

                if (!result.IsSslOk)
                {
                    result.ErrorMessage = BuildSslError(sslResult, config.SslExpireDays);
                }
            }
            else
            {
                result.IsSslOk = true;
            }

            if (!result.IsStatusCodeOk)
            {
                result.ErrorMessage = $"Unexpected HTTP status code: {statusCode}.";
            }

            if (!result.IsResponseTimeOk)
            {
                result.ErrorMessage = $"Slow response time: {result.ResponseTimeMs} ms.";
            }
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsAvailable = false;
            result.IsStatusCodeOk = false;
            result.IsResponseTimeOk = false;
            result.IsSslOk = false;
            result.ErrorMessage = $"Request timeout after {config.TimeoutSeconds} seconds.";
            _logger.LogWarning(ex, "Timeout while checking {Url}", target.Url);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsAvailable = false;
            result.IsStatusCodeOk = false;
            result.IsResponseTimeOk = false;
            result.IsSslOk = false;
            result.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Error while checking {Url}", target.Url);
        }

        return result;
    }

    private static string BuildSslError(SslCheckResult sslResult, int expireWarningDays)
    {
        if (!string.IsNullOrWhiteSpace(sslResult.ErrorMessage))
        {
            return sslResult.ErrorMessage;
        }

        if (sslResult.DaysRemaining.HasValue && sslResult.DaysRemaining.Value < expireWarningDays)
        {
            return $"SSL certificate expires soon: {sslResult.DaysRemaining.Value} days remaining.";
        }

        return "SSL certificate is invalid.";
    }
}
