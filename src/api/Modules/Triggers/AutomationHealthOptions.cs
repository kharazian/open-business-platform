namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class AutomationHealthOptions
{
    public const string SectionName = "AutomationHealth";

    public int PendingAgeWarningSeconds { get; set; } = 300;

    public int DeadLetterWarningCount { get; set; } = 1;

    public int MonitorIntervalSeconds { get; set; } = 300;

    public bool MetricsEnabled { get; set; } = true;

    public string? MetricsToken { get; set; }

    public AutomationHealthOptions Normalize()
    {
        return new AutomationHealthOptions
        {
            PendingAgeWarningSeconds = Math.Clamp(PendingAgeWarningSeconds, 30, 86400),
            DeadLetterWarningCount = Math.Clamp(DeadLetterWarningCount, 1, 1000000),
            MonitorIntervalSeconds = Math.Clamp(MonitorIntervalSeconds, 30, 3600),
            MetricsEnabled = MetricsEnabled,
            MetricsToken = string.IsNullOrWhiteSpace(MetricsToken) ? null : MetricsToken.Trim()
        };
    }
}
