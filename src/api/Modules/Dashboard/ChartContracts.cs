namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class ChartWidgetTypes
{
    public const string NumberCard = "number_card";
    public const string BarChart = "bar_chart";
    public const string DateTrend = "date_trend";
    public const string ChoiceBreakdown = "choice_breakdown";
    public const string Table = "table";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NumberCard,
        BarChart,
        DateTrend,
        ChoiceBreakdown,
        Table
    };
}

public static class ChartMetricTypes
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

public sealed record ChartMetricDefinition(string Type, string? FieldId = null);

public sealed record ChartWidgetConfigDefinition(
    string WidgetType,
    ChartMetricDefinition Metric,
    string? GroupByFieldId = null,
    string? DateFieldId = null,
    IReadOnlyList<string>? Columns = null,
    int? Limit = null,
    Guid? ReportId = null);

public sealed record ChartSeriesPointDto(string Key, string Label, decimal Value);

public sealed record ChartTableColumnDto(string FieldId, string Label, string Type, string Source);

public sealed record ChartTableCellDto(object? Value, string DisplayValue);

public sealed record ChartTableRowDto(
    Guid RecordId,
    string Status,
    IReadOnlyDictionary<string, ChartTableCellDto> Cells,
    DateTimeOffset CreatedAt);

public sealed record ChartWidgetPreviewDto(
    Guid FormId,
    string FormName,
    string WidgetType,
    ChartMetricDefinition Metric,
    IReadOnlyList<ChartTableColumnDto> Columns,
    IReadOnlyList<ChartSeriesPointDto> Series,
    IReadOnlyList<ChartTableRowDto> Rows,
    long TotalCount);

public sealed record ChartValidationError(string Path, string Code, string Message);

public sealed record ChartValidationResult(IReadOnlyList<ChartValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record ChartErrorResponse(string Message, IReadOnlyList<ChartValidationError>? Errors = null);

public sealed class ChartAggregationException : Exception
{
    public ChartAggregationException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<ChartValidationError>();
    }

    public ChartAggregationException(int statusCode, string message, IReadOnlyList<ChartValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<ChartValidationError> Errors { get; }
}
