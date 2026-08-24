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
        ValidateAppearance(config.Appearance, errors);

        return new ChartValidationResult(errors);
    }

    private static void ValidateAppearance(DashboardChartAppearanceDefinition? appearance, ICollection<ChartValidationError> errors)
    {
        if (appearance is null) return;
        if (!DashboardChartPalettes.Supported.Contains(Normalize(appearance.Palette))) errors.Add(new("appearance.palette", "chart.appearance.palette_invalid", "Chart palette is not supported."));
        if (!DashboardCardAccents.Supported.Contains(Normalize(appearance.CardAccent))) errors.Add(new("appearance.cardAccent", "chart.appearance.accent_invalid", "Card accent is not supported."));
        if (!DashboardNumberFormats.Supported.Contains(Normalize(appearance.NumberFormat))) errors.Add(new("appearance.numberFormat", "chart.appearance.number_format_invalid", "Number format is not supported."));
        if (appearance.DecimalPlaces is < 0 or > 4) errors.Add(new("appearance.decimalPlaces", "chart.appearance.decimals_range", "Decimal places must be between zero and four."));
        var currencyCode = Normalize(appearance.CurrencyCode);
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsAsciiLetter)) errors.Add(new("appearance.currencyCode", "chart.appearance.currency_invalid", "Currency code must contain three letters."));
    }

    private static void ValidateSeries(ChartWidgetConfigDefinition config, IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById, ICollection<ChartValidationError> errors)
    {
        var series = config.Series ?? Array.Empty<DashboardChartSeriesDefinition>();
        if (series.Count > 4) errors.Add(new("series", "chart.series.limit", "A chart supports at most four series."));
        if (Normalize(config.WidgetType) == ChartWidgetTypes.Table && series.Count > 1) errors.Add(new("series", "chart.series.table_unsupported", "Table widgets support one metric only."));
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
