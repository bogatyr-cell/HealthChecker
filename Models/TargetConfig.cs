namespace HealthChecker.Models;

public sealed class TargetConfig
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public List<int> SuccessCodes { get; set; } = new() { 200 };

    public int MaxResponseTimeMs { get; set; }

    public bool Enabled { get; set; } = true;
}
