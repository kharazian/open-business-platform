using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardAnalyticsRequestValidator
{
    public static DashboardAnalyticsValidationResult Validate(FormSchemaDefinition schema, DashboardAnalyticsRequest? request)
    {
        var errors = new List<DashboardAnalyticsValidationError>();

        if (request is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("request", "dashboard.analytics.request.required", "Dashboard analytics request is required."));
            return new DashboardAnalyticsValidationResult(errors);
        }

        if (request.Source is null || request.Source.FormId == Guid.Empty)
        {
            errors.Add(new DashboardAnalyticsValidationError("source.formId", "dashboard.analytics.source.form_required", "Source form is required."));
        }

        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema);
        var widgetType = Normalize(request.WidgetType);
        var metricType = Normalize(request.Metric?.Type);
        var limit = request.Limit ?? 10;

        if (!DashboardAnalyticsWidgetTypes.Supported.Contains(widgetType))
        {
            errors.Add(new DashboardAnalyticsValidationError("widgetType", "dashboard.analytics.widget_type.unsupported", "Choose a supported dashboard analytics widget type."));
        }

        if (!DashboardAnalyticsMetricTypes.Supported.Contains(metricType))
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.type", "dashboard.analytics.metric.unsupported", "Choose a supported dashboard analytics metric."));
        }

        if (limit is < 1 or > 50)
        {
            errors.Add(new DashboardAnalyticsValidationError("limit", "dashboard.analytics.limit.range", "Limit must be between 1 and 50."));
        }

        ValidateMetricField(request, fieldsById, errors);
        ValidateWidgetFields(request, fieldsById, errors);

        return new DashboardAnalyticsValidationResult(errors);
    }

    private static void ValidateMetricField(
        DashboardAnalyticsRequest request,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var metricType = Normalize(request.Metric?.Type);
        var fieldId = NormalizeOptional(request.Metric?.FieldId);

        if (metricType == DashboardAnalyticsMetricTypes.Count)
        {
            return;
        }

        if (fieldId is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.fieldId", "dashboard.analytics.metric.field_required", "Sum and average metrics require a numeric field."));
            return;
        }

        if (!fieldsById.TryGetValue(fieldId, out var field) || !field.SupportsAggregation)
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.fieldId", "dashboard.analytics.metric.field_invalid", "Metric field must be a reportable numeric field."));
        }
    }

    private static void ValidateWidgetFields(
        DashboardAnalyticsRequest request,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        switch (Normalize(request.WidgetType))
        {
            case DashboardAnalyticsWidgetTypes.Breakdown:
                ValidateGroupField(request.GroupByFieldId, fieldsById, errors);
                break;
            case DashboardAnalyticsWidgetTypes.Trend:
                ValidateDateField(request.DateFieldId, fieldsById, errors);
                break;
            case DashboardAnalyticsWidgetTypes.Table:
                ValidateColumns(request.Columns, fieldsById, errors);
                break;
        }
    }

    private static void ValidateGroupField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("groupByFieldId", "dashboard.analytics.group.field_required", "Breakdown widgets require a grouping field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || !field.SupportsChoiceGrouping)
        {
            errors.Add(new DashboardAnalyticsValidationError("groupByFieldId", "dashboard.analytics.group.field_invalid", "Grouping field must be a status or choice field."));
        }
    }

    private static void ValidateDateField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("dateFieldId", "dashboard.analytics.date.field_required", "Trend widgets require a date field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || field.Type is not (FormFieldTypes.Date or "datetime"))
        {
            errors.Add(new DashboardAnalyticsValidationError("dateFieldId", "dashboard.analytics.date.field_invalid", "Trend field must be a date field."));
        }
    }

    private static void ValidateColumns(
        IReadOnlyList<string>? columns,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        foreach (var fieldId in columns ?? Array.Empty<string>())
        {
            if (!fieldsById.ContainsKey(fieldId.Trim()))
            {
                errors.Add(new DashboardAnalyticsValidationError("columns", "dashboard.analytics.columns.field_invalid", "Table columns must use reportable fields."));
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
