using HealthChecker.Models;

namespace HealthChecker.Utils;

public interface IConfigLoader
{
    AppConfig Load(string path = "config.yaml");
}
