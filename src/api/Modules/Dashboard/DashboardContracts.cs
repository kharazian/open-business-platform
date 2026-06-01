namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public sealed record DashboardSummaryResponse(
    string Title,
    IReadOnlyCollection<DashboardMetric> Metrics,
    IReadOnlyCollection<DashboardActivityItem> RecentActivity);

public sealed record DashboardMetric(string Key, string Label, int Value);

public sealed record DashboardActivityItem(Guid Id, string Event, string Actor, DateTimeOffset CreatedAt, string Status);
