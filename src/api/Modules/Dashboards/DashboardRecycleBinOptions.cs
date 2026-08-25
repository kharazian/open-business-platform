namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public sealed class DashboardRecycleBinOptions
{
    public const string SectionName = "DashboardRecycleBin";

    public int PermanentDeleteMinimumAgeDays { get; set; } = 30;

    public int GetBoundedMinimumAgeDays() => Math.Clamp(PermanentDeleteMinimumAgeDays, 0, 3650);
}
