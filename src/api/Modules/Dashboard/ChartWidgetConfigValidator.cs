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

        return new ChartValidationResult(errors);
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

        if (!fieldsById.TryGetValue(normalized, out var field) || field.Type is not (FormFieldTypes.Date or "datetime"))
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
