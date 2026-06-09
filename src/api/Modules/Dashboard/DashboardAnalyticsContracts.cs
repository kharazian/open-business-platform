namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardAnalyticsWidgetTypes
{
    public const string Summary = "summary";
    public const string Breakdown = "breakdown";
    public const string Trend = "trend";
    public const string Table = "table";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Summary,
        Breakdown,
        Trend,
        Table
    };
}

public static class DashboardAnalyticsMetricTypes
{
    public const string Count = "count";
    public const string Sum = "sum";
    public const string Average = "average";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Count,
        Sum,
        Average
    };
}

public sealed record DashboardAnalyticsSourceDefinition(Guid FormId, Guid? ReportId = null);

public sealed record DashboardAnalyticsMetricDefinition(string Type, string? FieldId = null);

public sealed record DashboardAnalyticsRequest(
    string WidgetType,
    DashboardAnalyticsSourceDefinition Source,
    DashboardAnalyticsMetricDefinition Metric,
    string? GroupByFieldId = null,
    string? DateFieldId = null,
    IReadOnlyList<string>? Columns = null,
    int? Limit = null);

public sealed record DashboardAnalyticsResponse(
    Guid FormId,
    string FormName,
    Guid? ReportId,
    string WidgetType,
    DashboardAnalyticsMetricDefinition Metric,
    IReadOnlyList<ChartSeriesPointDto> Series,
    IReadOnlyList<ChartTableColumnDto> Columns,
    IReadOnlyList<ChartTableRowDto> Rows,
    long TotalCount);

public sealed record DashboardAnalyticsValidationError(string Path, string Code, string Message);

public sealed record DashboardAnalyticsValidationResult(IReadOnlyList<DashboardAnalyticsValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record DashboardAnalyticsErrorResponse(
    string Message,
    IReadOnlyList<DashboardAnalyticsValidationError>? Errors = null);

public sealed class DashboardAnalyticsException : Exception
{
    public DashboardAnalyticsException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<DashboardAnalyticsValidationError>();
    }

    public DashboardAnalyticsException(int statusCode, string message, IReadOnlyList<DashboardAnalyticsValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<DashboardAnalyticsValidationError> Errors { get; }
}
