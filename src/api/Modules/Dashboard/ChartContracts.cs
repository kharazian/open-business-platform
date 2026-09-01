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

public static class DashboardSeriesDisplayTypes
{
    public const string Pie = "pie";
    public const string Donut = "donut";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "bar", "line", "area", Pie, Donut };

    public static bool IsCircular(string value) => value is Pie or Donut;
}

public static class DashboardSeriesAxes
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "left", "right" };
}

public static class DashboardSeriesColors
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "primary", "info", "success", "warning", "danger", "violet" };
}

public sealed record DashboardChartSeriesDefinition(string Id, string Label, ChartMetricDefinition Metric, string DisplayType = "bar", string Color = "primary", string Axis = "left");

public static class DashboardChartPalettes
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "theme", "cool", "warm", "mono" };
}

public static class DashboardNumberFormats
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "auto", "number", "currency", "percent" };
}

public static class DashboardCardAccents
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "none", "primary", "info", "success", "warning", "danger", "violet" };
}

public static class DashboardConditionalOperators
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "greater_than", "greater_or_equal", "less_than", "less_or_equal", "equal" };
}

public static class DashboardKpiGoalDirections
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "higher_is_better", "lower_is_better" };
}

public static class DashboardReferenceLineStyles
{
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { "solid", "dashed", "dotted" };
}

public static class DashboardBarModes
{
    public const string Grouped = "grouped";
    public const string Stacked = "stacked";
    public const string StackedPercent = "stacked_percent";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Grouped, Stacked, StackedPercent };
}

public static class DashboardBarOrientations
{
    public const string Vertical = "vertical";
    public const string Horizontal = "horizontal";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Vertical, Horizontal };
}

public static class DashboardCategorySorts
{
    public const string Source = "source";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Source, "value_desc", "value_asc", "label_asc", "label_desc" };
}

public static class DashboardLegendPositions
{
    public const string Top = "top";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Top, "bottom", "left", "right" };
}

public static class DashboardDataLabelContents
{
    public const string Auto = "auto";
    public const string Value = "value";
    public const string Percentage = "percentage";
    public const string ValueAndPercentage = "value_and_percentage";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Auto, Value, Percentage, ValueAndPercentage };
}

public static class DashboardDataLabelPositions
{
    public const string Auto = "auto";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Auto, "inside", "outside" };
}

public sealed record DashboardConditionalRuleDefinition(string Id, string Operator, decimal Value, string Accent, string? Label = null);
public sealed record DashboardConditionalFormattingDefinition(bool Enabled = false, IReadOnlyList<DashboardConditionalRuleDefinition>? Rules = null);
public sealed record DashboardKpiTargetDefinition(bool Enabled = false, decimal Value = 0, string Label = "Target", string Direction = "higher_is_better");
public sealed record DashboardKpiComparisonDefinition(bool Enabled = false, string DateFieldId = "", string Period = "last_30_days");
public sealed record DashboardReferenceLineDefinition(string Id, string Label, decimal Value, string Color = "warning", string Style = "dashed", string Axis = "left");
public sealed record DashboardAxisAppearanceDefinition(string Title = "", decimal? Maximum = null);
public sealed record DashboardChartAxesDefinition(DashboardAxisAppearanceDefinition? Left = null, DashboardAxisAppearanceDefinition? Right = null);

public sealed record DashboardChartAppearanceDefinition(
    string Palette = "theme",
    bool ShowLegend = true,
    bool ShowDataLabels = false,
    bool ShowGridlines = true,
    string CardAccent = "none",
    string NumberFormat = "auto",
    string CurrencyCode = "CAD",
    int DecimalPlaces = 0,
    DashboardConditionalFormattingDefinition? ConditionalFormatting = null,
    DashboardKpiTargetDefinition? KpiTarget = null,
    IReadOnlyList<DashboardReferenceLineDefinition>? ReferenceLines = null,
    string BarMode = "grouped",
    string BarOrientation = "vertical",
    string CategorySort = "source",
    string LegendPosition = "top",
    string DataLabelContent = "auto",
    string DataLabelPosition = "auto",
    DashboardChartAxesDefinition? Axes = null);

public sealed record ChartWidgetConfigDefinition(
    string WidgetType,
    ChartMetricDefinition Metric,
    string? GroupByFieldId = null,
    string? DateFieldId = null,
    IReadOnlyList<string>? Columns = null,
    int? Limit = null,
    Guid? ReportId = null,
    IReadOnlyList<DashboardChartSeriesDefinition>? Series = null,
    DashboardChartAppearanceDefinition? Appearance = null,
    IReadOnlyList<DashboardAnalyticsFilterDefinition>? FixedFilters = null,
    DashboardKpiComparisonDefinition? KpiComparison = null);

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
