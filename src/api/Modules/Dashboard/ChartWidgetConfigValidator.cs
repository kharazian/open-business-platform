using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class ChartWidgetConfigValidator
{
    public static ChartValidationResult Validate(FormSchemaDefinition schema, ChartWidgetConfigDefinition? config)
    {
        var errors = new List<ChartValidationError>();

        if (config is null)
        {
            errors.Add(new ChartValidationError("config", "chart.config.required", "Chart config is required."));
            return new ChartValidationResult(errors);
        }

        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema);
        var widgetType = Normalize(config.WidgetType);
        var metricType = Normalize(config.Metric?.Type);
        var limit = config.Limit ?? 10;

        if (!ChartWidgetTypes.Supported.Contains(widgetType))
        {
            errors.Add(new ChartValidationError("widgetType", "chart.widget_type.unsupported", "Choose a supported chart widget type."));
        }

        if (!ChartMetricTypes.Supported.Contains(metricType))
        {
            errors.Add(new ChartValidationError("metric.type", "chart.metric.unsupported", "Choose a supported chart metric."));
        }

        if (limit is < 1 or > 50)
        {
            errors.Add(new ChartValidationError("limit", "chart.limit.range", "Limit must be between 1 and 50."));
        }

        ValidateMetricField(config, fieldsById, errors);
        ValidateWidgetFields(config, fieldsById, errors);
        ValidateSeries(config, fieldsById, errors);
        ValidateAppearance(config.Appearance, widgetType, config.Metric, config.Series, errors);
        ValidateFixedFilters(config.FixedFilters, schema, errors);
        ValidateKpiComparison(config.KpiComparison, widgetType, fieldsById, errors);

        return new ChartValidationResult(errors);
    }

    private static void ValidateKpiComparison(DashboardKpiComparisonDefinition? comparison, string widgetType, IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById, ICollection<ChartValidationError> errors)
    {
        if (comparison is null || !comparison.Enabled) return;
        if (widgetType != ChartWidgetTypes.NumberCard) errors.Add(new("kpiComparison", "chart.kpi_comparison.widget_type_invalid", "Period comparison is supported only for KPI widgets."));
        var fieldId = Normalize(comparison.DateFieldId);
        if (!fieldsById.TryGetValue(fieldId, out var field) || field.Type is not (FormFieldTypes.Date or FormFieldTypes.Datetime)) errors.Add(new("kpiComparison.dateFieldId", "chart.kpi_comparison.date_field_invalid", "Period comparison requires a reportable date field."));
        if (!DashboardKpiComparisonPeriods.Days.ContainsKey(Normalize(comparison.Period))) errors.Add(new("kpiComparison.period", "chart.kpi_comparison.period_invalid", "Choose a supported comparison period."));
    }

    private static void ValidateFixedFilters(IReadOnlyList<DashboardAnalyticsFilterDefinition>? filters, FormSchemaDefinition schema, ICollection<ChartValidationError> errors)
    {
        var values = filters ?? Array.Empty<DashboardAnalyticsFilterDefinition>();
        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema);
        if (values.Count > 8) errors.Add(new("fixedFilters", "chart.fixed_filter.limit", "A chart supports at most eight fixed filters."));
        foreach (var duplicate in values.Select(filter => Normalize(filter.FieldId)).Where(fieldId => fieldId.Length > 0).GroupBy(fieldId => fieldId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add(new("fixedFilters", "chart.fixed_filter.duplicate_field", $"Fixed filter field '{duplicate.Key}' is duplicated."));
        }
        foreach (var error in DashboardAnalyticsRequestValidator.ValidateFilterValues(schema, values.Take(8).ToArray(), "fixedFilters").Errors)
        {
            errors.Add(new(error.Path, error.Code.Replace("dashboard.analytics.filter", "chart.fixed_filter", StringComparison.Ordinal), error.Message));
        }
        foreach (var item in values.Select((filter, index) => (filter, index)))
        {
            if ((item.filter.Values?.Count ?? 0) == 0 && string.IsNullOrWhiteSpace(item.filter.Start) && string.IsNullOrWhiteSpace(item.filter.End))
            {
                errors.Add(new($"fixedFilters[{item.index}]", "chart.fixed_filter.value_required", "A fixed filter requires at least one value or date bound."));
            }
            var fieldId = Normalize(item.filter.FieldId);
            if (fieldsById.TryGetValue(fieldId, out var field) && field.Options.Count > 0)
            {
                var allowedValues = field.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
                if ((item.filter.Values ?? Array.Empty<string>()).Any(value => !allowedValues.Contains(value)))
                {
                    errors.Add(new($"fixedFilters[{item.index}].values", "chart.fixed_filter.option_invalid", "Fixed filter values must use an option declared by the field."));
                }
            }
            if (fieldsById.TryGetValue(fieldId, out field) && field.Type is FormFieldTypes.Date or FormFieldTypes.Datetime)
            {
                if ((item.filter.Values?.Count ?? 0) > 0) errors.Add(new($"fixedFilters[{item.index}].values", "chart.fixed_filter.date_values_invalid", "Fixed date filters use start and end bounds instead of values."));
                if (DateTimeOffset.TryParse(item.filter.Start, out var start) && DateTimeOffset.TryParse(item.filter.End, out var end) && start >= end)
                {
                    errors.Add(new($"fixedFilters[{item.index}]", "chart.fixed_filter.date_range_invalid", "Fixed filter end must be after start."));
                }
            }
        }
    }

    private static void ValidateAppearance(DashboardChartAppearanceDefinition? appearance, string widgetType, ChartMetricDefinition? primaryMetric, IReadOnlyList<DashboardChartSeriesDefinition>? series, ICollection<ChartValidationError> errors)
    {
        if (appearance is null) return;
        if (!DashboardChartPalettes.Supported.Contains(Normalize(appearance.Palette))) errors.Add(new("appearance.palette", "chart.appearance.palette_invalid", "Chart palette is not supported."));
        if (!DashboardCardAccents.Supported.Contains(Normalize(appearance.CardAccent))) errors.Add(new("appearance.cardAccent", "chart.appearance.accent_invalid", "Card accent is not supported."));
        if (!DashboardNumberFormats.Supported.Contains(Normalize(appearance.NumberFormat))) errors.Add(new("appearance.numberFormat", "chart.appearance.number_format_invalid", "Number format is not supported."));
        if (!DashboardDisplayUnits.Supported.Contains(Normalize(appearance.DisplayUnit))) errors.Add(new("appearance.displayUnit", "chart.appearance.display_unit_invalid", "Display unit is not supported."));
        if (appearance.DecimalPlaces is < 0 or > 4) errors.Add(new("appearance.decimalPlaces", "chart.appearance.decimals_range", "Decimal places must be between zero and four."));
        var currencyCode = Normalize(appearance.CurrencyCode);
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsAsciiLetter)) errors.Add(new("appearance.currencyCode", "chart.appearance.currency_invalid", "Currency code must contain three letters."));
        ValidateConditionalFormatting(appearance.ConditionalFormatting, widgetType, errors);
        ValidateKpiTarget(appearance.KpiTarget, widgetType, errors);
        ValidateReferenceLines(appearance.ReferenceLines, widgetType, series, errors);
        ValidateBarMode(appearance.BarMode, widgetType, series, appearance.ReferenceLines, errors);
        ValidateBarOrientation(appearance.BarOrientation, widgetType, series, errors);
        ValidateCategorySort(appearance.CategorySort, widgetType, errors);
        ValidateCategoryLimit(appearance.CategoryLimit, appearance.GroupRemainingCategories, widgetType, primaryMetric, series, errors);
        ValidateLegendPosition(appearance.LegendPosition, widgetType, errors);
        ValidateDataLabels(appearance.DataLabelContent, appearance.DataLabelPosition, widgetType, series, appearance.BarMode, errors);
        ValidateAxes(appearance.Axes, widgetType, series, appearance.ReferenceLines, appearance.BarMode, errors);
    }

    private static void ValidateCategoryLimit(int? limit, bool groupRemaining, string widgetType, ChartMetricDefinition? primaryMetric, IReadOnlyList<DashboardChartSeriesDefinition>? series, ICollection<ChartValidationError> errors)
    {
        if (limit is < 1 or > 12) errors.Add(new("appearance.categoryLimit", "chart.category_limit.range", "Visible category limit must be between 1 and 12."));
        if (widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown) && (limit is not null || groupRemaining)) errors.Add(new("appearance.categoryLimit", "chart.category_limit.widget_type_invalid", "Top categories are supported only for breakdown charts."));
        if (groupRemaining && limit is null) errors.Add(new("appearance.groupRemainingCategories", "chart.category_limit.other_requires_limit", "Other grouping requires a visible category limit."));
        var usesAverage = series is { Count: > 0 }
            ? series.Any(item => Normalize(item.Metric.Type) == ChartMetricTypes.Average)
            : Normalize(primaryMetric?.Type) == ChartMetricTypes.Average;
        if (groupRemaining && usesAverage) errors.Add(new("appearance.groupRemainingCategories", "chart.category_limit.other_average_invalid", "Other grouping cannot sum category averages."));
    }

    private static void ValidateDataLabels(string? contentInput, string? positionInput, string widgetType, IReadOnlyList<DashboardChartSeriesDefinition>? seriesInput, string? barModeInput, ICollection<ChartValidationError> errors)
    {
        var content = Normalize(contentInput);
        var position = Normalize(positionInput);
        if (!DashboardDataLabelContents.Supported.Contains(content)) errors.Add(new("appearance.dataLabelContent", "chart.data_label.content_invalid", "Data label content is not supported."));
        if (!DashboardDataLabelPositions.Supported.Contains(position)) errors.Add(new("appearance.dataLabelPosition", "chart.data_label.position_invalid", "Data label position is not supported."));
        if (!DashboardDataLabelContents.Supported.Contains(content) || !DashboardDataLabelPositions.Supported.Contains(position)) return;
        var chart = widgetType is ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown or ChartWidgetTypes.DateTrend;
        if (!chart && (content != DashboardDataLabelContents.Auto || position != DashboardDataLabelPositions.Auto)) errors.Add(new("appearance.dataLabels", "chart.data_label.widget_type_invalid", "Custom data labels are supported only for charts."));
        var circular = (seriesInput ?? Array.Empty<DashboardChartSeriesDefinition>()).Any(item => DashboardSeriesDisplayTypes.IsCircular(Normalize(item.DisplayType)));
        if (position != DashboardDataLabelPositions.Auto && circular) errors.Add(new("appearance.dataLabelPosition", "chart.data_label.circular_position_invalid", "Pie and donut labels use the collision-safe label list."));
        var percentageContent = content is DashboardDataLabelContents.Percentage or DashboardDataLabelContents.ValueAndPercentage;
        if (percentageContent && !circular && Normalize(barModeInput) != DashboardBarModes.StackedPercent) errors.Add(new("appearance.dataLabelContent", "chart.data_label.percentage_invalid", "Percentage labels require a pie, donut, or 100% stacked chart."));
    }

    private static void ValidateLegendPosition(string? positionInput, string widgetType, ICollection<ChartValidationError> errors)
    {
        var position = Normalize(positionInput);
        if (!DashboardLegendPositions.Supported.Contains(position)) { errors.Add(new("appearance.legendPosition", "chart.legend_position.invalid", "Legend position is not supported.")); return; }
        if (position != DashboardLegendPositions.Top && widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown or ChartWidgetTypes.DateTrend)) errors.Add(new("appearance.legendPosition", "chart.legend_position.widget_type_invalid", "Legend positioning is supported only for charts."));
    }

    private static void ValidateAxes(DashboardChartAxesDefinition? axesInput, string widgetType, IReadOnlyList<DashboardChartSeriesDefinition>? seriesInput, IReadOnlyList<DashboardReferenceLineDefinition>? referenceLines, string? barModeInput, ICollection<ChartValidationError> errors)
    {
        if (axesInput is null) return;
        var left = axesInput.Left ?? new DashboardAxisAppearanceDefinition();
        var right = axesInput.Right ?? new DashboardAxisAppearanceDefinition();
        var series = seriesInput ?? Array.Empty<DashboardChartSeriesDefinition>();
        var cartesian = widgetType is ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown or ChartWidgetTypes.DateTrend;
        if (series.Any(item => DashboardSeriesDisplayTypes.IsCircular(Normalize(item.DisplayType)))) cartesian = false;
        if (!cartesian && (!IsDefaultAxis(left) || !IsDefaultAxis(right))) errors.Add(new("appearance.axes", "chart.axes.widget_type_invalid", "Axis settings are supported only for cartesian charts."));
        var leftActive = series.Count == 0 || series.Any(item => Normalize(item.Axis) == "left") || (referenceLines ?? Array.Empty<DashboardReferenceLineDefinition>()).Any(line => Normalize(line.Axis) == "left");
        var rightActive = series.Any(item => Normalize(item.Axis) == "right") || (referenceLines ?? Array.Empty<DashboardReferenceLineDefinition>()).Any(line => Normalize(line.Axis) == "right");
        if (!leftActive && !IsDefaultAxis(left)) errors.Add(new("appearance.axes.left", "chart.axes.left_inactive", "Left-axis settings require a left-axis series or reference line."));
        if (!rightActive && !IsDefaultAxis(right)) errors.Add(new("appearance.axes.right", "chart.axes.right_inactive", "Right-axis settings require a right-axis series or reference line."));
        if (Normalize(barModeInput) == DashboardBarModes.StackedPercent && (left.Maximum is not null || right.Maximum is not null)) errors.Add(new("appearance.axes", "chart.axes.percent_maximum_invalid", "100% stacked charts use a fixed automatic maximum."));
        ValidateAxis(left, "left", errors);
        ValidateAxis(right, "right", errors);
    }

    private static void ValidateAxis(DashboardAxisAppearanceDefinition axis, string name, ICollection<ChartValidationError> errors)
    {
        if ((axis.Title?.Length ?? 0) > 80) errors.Add(new($"appearance.axes.{name}.title", "chart.axes.title_invalid", "Axis titles may contain at most 80 characters."));
        if (axis.Maximum is <= 0 or > 1_000_000_000_000_000m) errors.Add(new($"appearance.axes.{name}.maximum", "chart.axes.maximum_invalid", "Manual axis maximum must be greater than zero and within the supported numeric range."));
    }

    private static bool IsDefaultAxis(DashboardAxisAppearanceDefinition axis) => string.IsNullOrEmpty(axis.Title) && axis.Maximum is null;

    private static void ValidateCategorySort(string? sortInput, string widgetType, ICollection<ChartValidationError> errors)
    {
        var sort = Normalize(sortInput);
        if (!DashboardCategorySorts.Supported.Contains(sort)) { errors.Add(new("appearance.categorySort", "chart.category_sort.invalid", "Category order is not supported.")); return; }
        if (sort != DashboardCategorySorts.Source && widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown)) errors.Add(new("appearance.categorySort", "chart.category_sort.widget_type_invalid", "Category sorting is supported only for breakdown charts."));
    }

    private static void ValidateBarOrientation(string? orientationInput, string widgetType, IReadOnlyList<DashboardChartSeriesDefinition>? seriesInput, ICollection<ChartValidationError> errors)
    {
        var orientation = Normalize(orientationInput);
        if (!DashboardBarOrientations.Supported.Contains(orientation)) { errors.Add(new("appearance.barOrientation", "chart.bar_orientation.invalid", "Bar direction is not supported.")); return; }
        if (orientation == DashboardBarOrientations.Vertical) return;
        var series = seriesInput ?? Array.Empty<DashboardChartSeriesDefinition>();
        if (widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown)) errors.Add(new("appearance.barOrientation", "chart.bar_orientation.widget_type_invalid", "Horizontal bars are supported only for category breakdown charts."));
        if (series.Count == 0 || series.Any(item => Normalize(item.DisplayType) != "bar")) errors.Add(new("appearance.barOrientation", "chart.bar_orientation.bar_series_required", "Horizontal charts require Bar series."));
        if (series.Select(item => Normalize(item.Axis)).Distinct(StringComparer.Ordinal).Count() > 1) errors.Add(new("appearance.barOrientation", "chart.bar_orientation.axis_mismatch", "Horizontal Bar series must use the same axis."));
    }

    private static void ValidateBarMode(string? barModeInput, string widgetType, IReadOnlyList<DashboardChartSeriesDefinition>? seriesInput, IReadOnlyList<DashboardReferenceLineDefinition>? referenceLines, ICollection<ChartValidationError> errors)
    {
        var barMode = Normalize(barModeInput);
        if (!DashboardBarModes.Supported.Contains(barMode)) { errors.Add(new("appearance.barMode", "chart.bar_mode.invalid", "Bar layout is not supported.")); return; }
        if (barMode == DashboardBarModes.Grouped) return;
        var series = seriesInput ?? Array.Empty<DashboardChartSeriesDefinition>();
        if (widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown or ChartWidgetTypes.DateTrend)) errors.Add(new("appearance.barMode", "chart.bar_mode.widget_type_invalid", "Stacked bars are supported only for breakdown and trend charts."));
        if (series.Count < 2) errors.Add(new("appearance.barMode", "chart.bar_mode.series_required", "Stacked bars require at least two series."));
        if (series.Any(item => Normalize(item.DisplayType) != "bar")) errors.Add(new("appearance.barMode", "chart.bar_mode.bar_series_required", "Every stacked series must use the Bar display type."));
        if (series.Select(item => Normalize(item.Axis)).Distinct(StringComparer.Ordinal).Count() > 1) errors.Add(new("appearance.barMode", "chart.bar_mode.axis_mismatch", "Stacked series must use the same axis."));
        if (barMode == DashboardBarModes.StackedPercent && (referenceLines?.Count ?? 0) > 0) errors.Add(new("appearance.barMode", "chart.bar_mode.percent_reference_invalid", "100% stacked bars do not support reference lines."));
    }

    private static void ValidateReferenceLines(IReadOnlyList<DashboardReferenceLineDefinition>? referenceLines, string widgetType, IReadOnlyList<DashboardChartSeriesDefinition>? series, ICollection<ChartValidationError> errors)
    {
        var lines = referenceLines ?? Array.Empty<DashboardReferenceLineDefinition>();
        if (lines.Count == 0) return;
        if (widgetType is not (ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown or ChartWidgetTypes.DateTrend)) errors.Add(new("appearance.referenceLines", "chart.reference_line.widget_type_invalid", "Reference lines are supported only for bar, line, and area charts."));
        if ((series ?? Array.Empty<DashboardChartSeriesDefinition>()).Any(item => DashboardSeriesDisplayTypes.IsCircular(Normalize(item.DisplayType)))) errors.Add(new("appearance.referenceLines", "chart.reference_line.circular_invalid", "Pie and donut charts do not support reference lines."));
        if (lines.Count > 4) errors.Add(new("appearance.referenceLines", "chart.reference_line.limit", "A chart supports at most four reference lines."));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in lines.Select((line, index) => (line, index)))
        {
            var path = $"appearance.referenceLines[{item.index}]";
            var id = Normalize(item.line.Id);
            if (id.Length is < 1 or > 50 || !id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')) errors.Add(new($"{path}.id", "chart.reference_line.id_invalid", "Reference line id must contain 1-50 letters, numbers, hyphens, or underscores."));
            else if (!ids.Add(id)) errors.Add(new("appearance.referenceLines", "chart.reference_line.duplicate_id", "Reference line ids must be unique."));
            if (string.IsNullOrWhiteSpace(item.line.Label) || item.line.Label.Length > 80) errors.Add(new($"{path}.label", "chart.reference_line.label_invalid", "Reference line label must contain 1-80 characters."));
            if (item.line.Value < 0 || item.line.Value > 1_000_000_000_000_000m) errors.Add(new($"{path}.value", "chart.reference_line.value_range", "Reference line value must be between zero and the supported maximum."));
            if (!DashboardSeriesColors.Supported.Contains(Normalize(item.line.Color))) errors.Add(new($"{path}.color", "chart.reference_line.color_invalid", "Reference line color is not supported."));
            if (!DashboardReferenceLineStyles.Supported.Contains(Normalize(item.line.Style))) errors.Add(new($"{path}.style", "chart.reference_line.style_invalid", "Reference line style is not supported."));
            if (!DashboardSeriesAxes.Supported.Contains(Normalize(item.line.Axis))) errors.Add(new($"{path}.axis", "chart.reference_line.axis_invalid", "Reference line axis is not supported."));
        }
    }

    private static void ValidateKpiTarget(DashboardKpiTargetDefinition? target, string widgetType, ICollection<ChartValidationError> errors)
    {
        if (target is null || !target.Enabled) return;
        if (widgetType != ChartWidgetTypes.NumberCard) errors.Add(new("appearance.kpiTarget", "chart.kpi_target.widget_type_invalid", "KPI targets are supported only for KPI widgets."));
        if (Math.Abs(target.Value) > 1_000_000_000_000_000m) errors.Add(new("appearance.kpiTarget.value", "chart.kpi_target.value_range", "KPI target is outside the supported range."));
        if (string.IsNullOrWhiteSpace(target.Label)) errors.Add(new("appearance.kpiTarget.label", "chart.kpi_target.label_required", "Enabled KPI targets require a label."));
        else if (target.Label.Length > 80) errors.Add(new("appearance.kpiTarget.label", "chart.kpi_target.label_invalid", "KPI target label must contain at most 80 characters."));
        if (!DashboardKpiGoalDirections.Supported.Contains(Normalize(target.Direction))) errors.Add(new("appearance.kpiTarget.direction", "chart.kpi_target.direction_invalid", "KPI goal direction is not supported."));
    }

    private static void ValidateConditionalFormatting(DashboardConditionalFormattingDefinition? formatting, string widgetType, ICollection<ChartValidationError> errors)
    {
        if (formatting is null) return;
        var rules = formatting.Rules ?? Array.Empty<DashboardConditionalRuleDefinition>();
        if (rules.Count > 5) errors.Add(new("appearance.conditionalFormatting.rules", "chart.conditional.rule_limit", "Conditional formatting supports at most five rules."));
        if (formatting.Enabled && widgetType != ChartWidgetTypes.NumberCard) errors.Add(new("appearance.conditionalFormatting", "chart.conditional.widget_type_invalid", "Conditional formatting is supported only for KPI widgets."));
        if (formatting.Enabled && rules.Count == 0) errors.Add(new("appearance.conditionalFormatting.rules", "chart.conditional.rule_required", "Enabled conditional formatting requires at least one rule."));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in rules.Select((rule, index) => (rule, index)))
        {
            var path = $"appearance.conditionalFormatting.rules[{item.index}]";
            var id = Normalize(item.rule.Id);
            if (id.Length is < 1 or > 50 || !id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')) errors.Add(new($"{path}.id", "chart.conditional.id_invalid", "Conditional rule id must contain 1-50 letters, numbers, hyphens, or underscores."));
            else if (!ids.Add(id)) errors.Add(new("appearance.conditionalFormatting.rules", "chart.conditional.duplicate_id", "Conditional rule ids must be unique."));
            if (!DashboardConditionalOperators.Supported.Contains(Normalize(item.rule.Operator))) errors.Add(new($"{path}.operator", "chart.conditional.operator_invalid", "Conditional rule operator is not supported."));
            if (Math.Abs(item.rule.Value) > 1_000_000_000_000_000m) errors.Add(new($"{path}.value", "chart.conditional.value_range", "Conditional threshold is outside the supported range."));
            var accent = Normalize(item.rule.Accent);
            if (accent == "none" || !DashboardCardAccents.Supported.Contains(accent)) errors.Add(new($"{path}.accent", "chart.conditional.accent_invalid", "Conditional rule requires a semantic accent color."));
            if (formatting.Enabled && string.IsNullOrWhiteSpace(item.rule.Label)) errors.Add(new($"{path}.label", "chart.conditional.label_required", "Enabled conditional rules require a status label."));
            else if ((item.rule.Label?.Length ?? 0) > 80) errors.Add(new($"{path}.label", "chart.conditional.label_invalid", "Conditional rule label must contain at most 80 characters."));
        }
    }

    private static void ValidateSeries(ChartWidgetConfigDefinition config, IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById, ICollection<ChartValidationError> errors)
    {
        var series = config.Series ?? Array.Empty<DashboardChartSeriesDefinition>();
        var widgetType = Normalize(config.WidgetType);
        if (series.Count > 4) errors.Add(new("series", "chart.series.limit", "A chart supports at most four series."));
        if (widgetType == ChartWidgetTypes.Table && series.Count > 1) errors.Add(new("series", "chart.series.table_unsupported", "Table widgets support one metric only."));
        var circularSeries = series.Where(item => DashboardSeriesDisplayTypes.IsCircular(Normalize(item.DisplayType))).ToArray();
        if (circularSeries.Length > 0 && widgetType != ChartWidgetTypes.ChoiceBreakdown) errors.Add(new("series", "chart.series.circular_widget_type_invalid", "Pie and donut displays require a category breakdown widget."));
        if (circularSeries.Length > 0 && series.Count != 1) errors.Add(new("series", "chart.series.circular_single_series_required", "Pie and donut displays support exactly one series."));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in series.Select((value, index) => (value, index)))
        {
            var path = $"series[{item.index}]";
            var id = Normalize(item.value.Id);
            if (id.Length is < 1 or > 50 || !id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')) errors.Add(new($"{path}.id", "chart.series.id_invalid", "Series id must contain 1-50 letters, numbers, hyphens, or underscores."));
            else if (!ids.Add(id)) errors.Add(new("series", "chart.series.duplicate_id", "Series ids must be unique."));
            if (string.IsNullOrWhiteSpace(item.value.Label) || item.value.Label.Length > 80) errors.Add(new($"{path}.label", "chart.series.label_invalid", "Series label must contain 1-80 characters."));
            if (!DashboardSeriesDisplayTypes.Supported.Contains(Normalize(item.value.DisplayType))) errors.Add(new($"{path}.displayType", "chart.series.display_invalid", "Series display type is not supported."));
            if (!DashboardSeriesColors.Supported.Contains(Normalize(item.value.Color))) errors.Add(new($"{path}.color", "chart.series.color_invalid", "Series color is not supported."));
            if (!DashboardSeriesAxes.Supported.Contains(Normalize(item.value.Axis))) errors.Add(new($"{path}.axis", "chart.series.axis_invalid", "Series axis is not supported."));
            ValidateMetric(item.value.Metric, fieldsById, path, errors);
        }
    }

    private static void ValidateMetric(ChartMetricDefinition? metric, IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById, string path, ICollection<ChartValidationError> errors)
    {
        var metricType = Normalize(metric?.Type);
        if (!ChartMetricTypes.Supported.Contains(metricType)) { errors.Add(new($"{path}.metric.type", "chart.metric.unsupported", "Choose a supported chart metric.")); return; }
        if (metricType == ChartMetricTypes.Count) return;
        var fieldId = NormalizeOptional(metric?.FieldId);
        if (fieldId is null || !fieldsById.TryGetValue(fieldId, out var field) || !field.SupportsAggregation) errors.Add(new($"{path}.metric.fieldId", "chart.metric.field_invalid", "Series sum and average require a reportable numeric field."));
    }

    private static void ValidateMetricField(
        ChartWidgetConfigDefinition config,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<ChartValidationError> errors)
    {
        var metricType = Normalize(config.Metric?.Type);
        var fieldId = NormalizeOptional(config.Metric?.FieldId);

        if (metricType == ChartMetricTypes.Count)
        {
            return;
        }

        if (fieldId is null)
        {
            errors.Add(new ChartValidationError("metric.fieldId", "chart.metric.field_required", "Sum and average metrics require a numeric field."));
            return;
        }

        if (!fieldsById.TryGetValue(fieldId, out var field) || !field.SupportsAggregation)
        {
            errors.Add(new ChartValidationError("metric.fieldId", "chart.metric.field_invalid", "Metric field must be a reportable numeric field."));
        }
    }

    private static void ValidateWidgetFields(
        ChartWidgetConfigDefinition config,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<ChartValidationError> errors)
    {
        switch (Normalize(config.WidgetType))
        {
            case ChartWidgetTypes.BarChart:
            case ChartWidgetTypes.ChoiceBreakdown:
                ValidateGroupField(config.GroupByFieldId, fieldsById, errors);
                break;
            case ChartWidgetTypes.DateTrend:
                ValidateDateField(config.DateFieldId, fieldsById, errors);
                break;
            case ChartWidgetTypes.Table:
                ValidateColumns(config.Columns, fieldsById, errors);
                break;
        }
    }

    private static void ValidateGroupField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<ChartValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new ChartValidationError("groupByFieldId", "chart.group.field_required", "Grouped charts require a grouping field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || !field.SupportsChoiceGrouping)
        {
            errors.Add(new ChartValidationError("groupByFieldId", "chart.group.field_invalid", "Grouping field must be a status or choice field."));
        }
    }

    private static void ValidateDateField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<ChartValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new ChartValidationError("dateFieldId", "chart.date.field_required", "Date trend charts require a date field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || field.Type is not (FormFieldTypes.Date or FormFieldTypes.Datetime))
        {
            errors.Add(new ChartValidationError("dateFieldId", "chart.date.field_invalid", "Date trend field must be a date field."));
        }
    }

    private static void ValidateColumns(
        IReadOnlyList<string>? columns,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<ChartValidationError> errors)
    {
        foreach (var fieldId in columns ?? Array.Empty<string>())
        {
            if (!fieldsById.ContainsKey(fieldId.Trim()))
            {
                errors.Add(new ChartValidationError("columns", "chart.columns.field_invalid", "Table columns must use reportable fields."));
            }
        }
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
