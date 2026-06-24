using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using HealthChecker.Models;

namespace HealthChecker.Services;

public sealed class SslChecker
{
    private readonly ILogger<SslChecker> _logger;

    public SslChecker(ILogger<SslChecker> logger)
    {
        _logger = logger;
    }

    public async Task<SslCheckResult> CheckAsync(
        string host,
        int port,
        int expireWarningDays,
        CancellationToken cancellationToken)
    {
        SslPolicyErrors policyErrors = SslPolicyErrors.None;

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            using var sslStream = new SslStream(
                tcpClient.GetStream(),
                false,
                (_, _, _, errors) =>
                {
                    policyErrors = errors;
                    return true;
                });

            await sslStream.AuthenticateAsClientAsync(host);

            if (sslStream.RemoteCertificate is null)
            {
                return new SslCheckResult
                {
                    IsValid = false,
                    IsExpiringSoon = false,
                    ErrorMessage = "SSL certificate was not received.",
                };
            }

            var certificate = new X509Certificate2(sslStream.RemoteCertificate);
            DateTimeOffset expiresAt = certificate.NotAfter;
            int daysRemaining = (int)Math.Floor((expiresAt - DateTimeOffset.UtcNow).TotalDays);
            bool hasPolicyErrors = policyErrors != SslPolicyErrors.None;
            bool expired = expiresAt <= DateTimeOffset.UtcNow;
            bool expiringSoon = daysRemaining < expireWarningDays;

            return new SslCheckResult
            {
                IsValid = !hasPolicyErrors && !expired,
                IsExpiringSoon = expiringSoon,
                DaysRemaining = daysRemaining,
                ExpiresAt = expiresAt,
                ErrorMessage = hasPolicyErrors ? $"SSL policy errors: {policyErrors}" : null,
            };
        }
        catch (Exception ex) when (ex is SocketException or IOException or AuthenticationException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "SSL check failed for {Host}:{Port}", host, port);
            return new SslCheckResult
            {
                IsValid = false,
                IsExpiringSoon = false,
                ErrorMessage = ex.Message,
            };
        }
    }
}
