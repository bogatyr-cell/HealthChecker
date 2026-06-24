using HealthChecker.Services;
using HealthChecker.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddSingleton<IConfigLoader, ConfigLoader>();
        services.AddSingleton<SslChecker>();
        services.AddSingleton<EndpointChecker>();
        services.AddSingleton<TelegramNotifier>();
        services.AddHostedService<HealthCheckWorker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddNLog("nlog.config");
    })
    .Build();

await host.RunAsync();
