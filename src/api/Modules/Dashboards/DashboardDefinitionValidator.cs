using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public static class DashboardDefinitionValidator
{
    public static DashboardValidationResult Validate(
        SavedDashboardConfigDefinition? config,
        SavedDashboardLayoutDefinition? layout,
        IReadOnlyCollection<DashboardSourceDefinition> sources)
    {
        var errors = new List<DashboardValidationError>();

        if (config is null)
        {
            errors.Add(new DashboardValidationError("config", "dashboard.config.required", "Dashboard config is required."));
        }

        if (layout is null)
        {
            errors.Add(new DashboardValidationError("layout", "dashboard.layout.required", "Dashboard layout is required."));
        }

        if (config is null || layout is null)
        {
            return new DashboardValidationResult(errors);
        }

        if (config.SchemaVersion != 1)
        {
            errors.Add(new DashboardValidationError("config.schemaVersion", "dashboard.config.schema_version", "Dashboard config schema version is not supported."));
        }

        if (layout.SchemaVersion != 1)
        {
            errors.Add(new DashboardValidationError("layout.schemaVersion", "dashboard.layout.schema_version", "Dashboard layout schema version is not supported."));
        }

        if (config.TemplateProvenance is { } provenance
            && (string.IsNullOrWhiteSpace(provenance.TemplateId)
                || provenance.TemplateId.Length > 100
                || provenance.TemplateVersion < 1
                || provenance.InstantiatedAt == default))
        {
            errors.Add(new DashboardValidationError("config.templateProvenance", "dashboard.template.provenance_invalid", "Template provenance is invalid."));
        }

        var widgets = config.Widgets ?? Array.Empty<SavedDashboardWidgetDefinition>();
        var layouts = layout.Widgets ?? Array.Empty<SavedDashboardWidgetLayoutDefinition>();
        var widgetIds = widgets.Select(widget => Normalize(widget.Id)).ToArray();
        var layoutIds = layouts.Select(item => Normalize(item.Id)).ToArray();
        var sections = config.Sections ?? Array.Empty<SavedDashboardSectionDefinition>();
        var sectionIds = sections.Select(section => Normalize(section.Id)).ToArray();
        var filters = config.Filters ?? Array.Empty<SavedDashboardFilterDefinition>();
        if (sections.Count > 16) errors.Add(new("config.sections", "dashboard.sections.limit", "A dashboard supports at most 16 sections."));
        if (widgets.Count > 48) errors.Add(new("config.widgets", "dashboard.widgets.limit", "A dashboard supports at most 48 widgets."));
        if (filters.Count > 8) errors.Add(new("config.filters", "dashboard.filters.limit", "A dashboard supports at most 8 shared filters."));
        foreach (var duplicate in filters.Select(filter => Normalize(filter.Id)).Where(id => id.Length > 0).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add(new("config.filters", "dashboard.filters.duplicate_id", $"Filter id '{duplicate.Key}' is duplicated."));
        }
        foreach (var filter in filters)
        {
            var source = sources.SingleOrDefault(candidate => candidate.FormId == filter.SourceFormId);
            var fields = source is null ? null : OpenBusinessPlatform.Api.Modules.Forms.FormReportableFieldMetadata.GetReportableFieldsById(source.Schema);
            ReportableFieldMetadata? field = null;
            if (fields is not null) fields.TryGetValue(filter.FieldId, out field);
            if (string.IsNullOrWhiteSpace(filter.Id) || filter.Id.Length > 100 || string.IsNullOrWhiteSpace(filter.Label) || filter.Label.Length > 100) errors.Add(new("config.filters", "dashboard.filter.invalid", "Each filter needs a bounded id and label."));
            if (filter.Type is not ("date_range" or "single_select" or "multi_select" or "record_status")) errors.Add(new("config.filters.type", "dashboard.filter.type_invalid", "Dashboard filter type is not supported."));
            if (field is null || !field.Filterable) errors.Add(new("config.filters.fieldId", "dashboard.filter.field_invalid", "Dashboard filter field must be reportable and filterable."));
            if (field is not null && filter.Type == "date_range" && field.Type is not (FormFieldTypes.Date or FormFieldTypes.Datetime)) errors.Add(new("config.filters.type", "dashboard.filter.type_mismatch", "Date-range filters require a date or datetime field."));
            if (field is not null && filter.Type != "date_range" && field.Type is FormFieldTypes.Date or FormFieldTypes.Datetime) errors.Add(new("config.filters.type", "dashboard.filter.type_mismatch", "Date and datetime fields require a date-range filter."));
            if (field is not null && filter.Type == "record_status" && field.Id != ReportableSystemFields.Status) errors.Add(new("config.filters.type", "dashboard.filter.type_mismatch", "Record-status filters require the record status field."));
            if ((filter.Options?.Count ?? 0) > 20 || (filter.Options?.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 100) ?? false) || (filter.Options?.Distinct(StringComparer.Ordinal).Count() ?? 0) != (filter.Options?.Count ?? 0)) errors.Add(new("config.filters.options", "dashboard.filter.options_invalid", "Dashboard filter options must contain at most 20 unique, bounded values."));
            if ((filter.ApplyToWidgetIds ?? Array.Empty<string>()).Any(id => !widgetIds.Contains(id, StringComparer.Ordinal))) errors.Add(new("config.filters.applyToWidgetIds", "dashboard.filter.widget_missing", "Dashboard filter targets an unknown widget."));
            if ((filter.ApplyToWidgetIds ?? Array.Empty<string>()).Any(id => widgets.Any(widget => widget.Id == id && widget.SourceFormId != filter.SourceFormId))) errors.Add(new("config.filters.applyToWidgetIds", "dashboard.filter.widget_source_mismatch", "Dashboard filters can target only widgets with the same source form."));
            ValidateFilterDefault(filter, errors);
        }

        foreach (var duplicate in sectionIds.Where(id => id.Length > 0).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add(new DashboardValidationError("config.sections", "dashboard.sections.duplicate_id", $"Section id '{duplicate.Key}' is duplicated."));
        }

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Id) || section.Id.Length > 100 || string.IsNullOrWhiteSpace(section.Title) || section.Title.Length > 100)
            {
                errors.Add(new DashboardValidationError("config.sections", "dashboard.section.invalid", "Each dashboard section needs an id and title."));
            }
            if (section.Icon is not null && !DashboardSectionIcons.Supported.Contains(Normalize(section.Icon)))
            {
                errors.Add(new DashboardValidationError("config.sections.icon", "dashboard.section.icon_invalid", "Dashboard section icon is not supported."));
            }
        }

        foreach (var group in widgets.Where(widget => !string.IsNullOrWhiteSpace(widget.SectionId)).GroupBy(widget => Normalize(widget.SectionId)))
        {
            if (group.Count() > 16) errors.Add(new("config.widgets", "dashboard.section.widget_limit", $"Section '{group.Key}' supports at most 16 widgets."));
        }

        if (widgets.Count == 0)
        {
            errors.Add(new DashboardValidationError("config.widgets", "dashboard.widgets.required", "Add at least one dashboard widget."));
        }

        foreach (var duplicate in widgetIds.Where(id => id.Length > 0).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add(new DashboardValidationError("config.widgets", "dashboard.widgets.duplicate_id", $"Widget id '{duplicate.Key}' is duplicated."));
        }

        foreach (var widget in widgets)
        {
            ValidateWidget(widget, sources, sectionIds, errors);
        }

        foreach (var item in layouts)
        {
            var layoutId = Normalize(item.Id);

            if (!widgetIds.Contains(layoutId, StringComparer.Ordinal))
            {
                errors.Add(new DashboardValidationError("layout.widgets", "dashboard.layout.widget_missing", "Layout widgets must match config widgets."));
            }

            if (!DashboardWidgetWidths.Supported.Contains(Normalize(item.Width)))
            {
                errors.Add(new DashboardValidationError("layout.widgets.width", "dashboard.layout.width_invalid", "Dashboard widget width is not supported."));
            }
        }

        foreach (var widgetId in widgetIds.Where(id => id.Length > 0))
        {
            if (!layoutIds.Contains(widgetId, StringComparer.Ordinal))
            {
                errors.Add(new DashboardValidationError("layout.widgets", "dashboard.layout.widget_required", "Every config widget must have a layout entry."));
            }
        }

        return new DashboardValidationResult(errors);
    }

    private static void ValidateWidget(
        SavedDashboardWidgetDefinition widget,
        IReadOnlyCollection<DashboardSourceDefinition> sources,
        IReadOnlyCollection<string> sectionIds,
        ICollection<DashboardValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(widget.Id))
        {
            errors.Add(new DashboardValidationError("config.widgets.id", "dashboard.widget.id_required", "Widget id is required."));
        }

        if (string.IsNullOrWhiteSpace(widget.Title))
        {
            errors.Add(new DashboardValidationError("config.widgets.title", "dashboard.widget.title_required", "Widget title is required."));
        }

        if ((widget.Title?.Length ?? 0) > 160 || (widget.Subtitle?.Length ?? 0) > 300)
        {
            errors.Add(new DashboardValidationError("config.widgets.title", "dashboard.widget.text_too_long", "Widget title or subtitle is too long."));
        }

        if (!string.IsNullOrWhiteSpace(widget.SectionId) && !sectionIds.Contains(Normalize(widget.SectionId), StringComparer.Ordinal))
        {
            errors.Add(new DashboardValidationError("config.widgets.sectionId", "dashboard.widget.section_missing", "Widget section was not found."));
        }

        if (widget.Adapter is not null)
        {
            ValidateAdapter(widget.Adapter, errors);
            if (widget.Chart is not null)
            {
                errors.Add(new DashboardValidationError("config.widgets", "dashboard.widget.source_ambiguous", "A widget cannot use analytics and an adapter at the same time."));
            }
            return;
        }

        if (widget.Chart is null)
        {
            errors.Add(new DashboardValidationError("config.widgets.chart", "dashboard.widget.config_required", "Widget analytics or adapter configuration is required."));
            return;
        }

        if (widget.SourceFormId is null)
        {
            errors.Add(new DashboardValidationError("config.widgets.sourceFormId", "dashboard.widget.form_required", "Analytics widgets require a source form."));
            return;
        }

        var source = sources.SingleOrDefault(candidate => candidate.FormId == widget.SourceFormId.Value);

        if (source is null)
        {
            errors.Add(new DashboardValidationError("config.widgets.sourceFormId", "dashboard.widget.form_missing", "Widget source form was not found."));
            return;
        }

        if (widget.Chart.ReportId is not null
            && !source.Reports.Any(report => report.Id == widget.Chart.ReportId && report.Type == ReportTypes.List))
        {
            errors.Add(new DashboardValidationError("config.widgets.chart.reportId", "dashboard.widget.report_missing", "Widget source report was not found for the selected form."));
        }

        var chartValidation = ChartWidgetConfigValidator.Validate(source.Schema, widget.Chart);

        foreach (var error in chartValidation.Errors)
        {
            errors.Add(new DashboardValidationError($"config.widgets.chart.{error.Path}", error.Code, error.Message));
        }
    }

    private static class DashboardSectionIcons
    {
        public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            "activity", "badge-dollar-sign", "chart-column", "clipboard-list", "factory", "gauge",
            "heart-pulse", "package-check", "shield-check", "trending-up", "wrench"
        };
    }

    private static void ValidateAdapter(DashboardAdapterWidgetDefinition adapter, ICollection<DashboardValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(adapter.AdapterId) || string.IsNullOrWhiteSpace(adapter.VisualizationId))
        {
            errors.Add(new DashboardValidationError("config.widgets.adapter", "dashboard.adapter.required", "Adapter and visualization ids are required."));
        }

        foreach (var (key, value) in adapter.Settings ?? new Dictionary<string, object?>())
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 80 || IsSecretKey(key) || !IsSafeSetting(value))
            {
                errors.Add(new DashboardValidationError("config.widgets.adapter.settings", "dashboard.adapter.setting_unsafe", "Adapter settings may contain only safe scalar configuration values."));
            }
        }
    }

    private static void ValidateFilterDefault(SavedDashboardFilterDefinition filter, ICollection<DashboardValidationError> errors)
    {
        if (filter.DefaultValue is not { } value) return;
        var values = value.Values ?? Array.Empty<string>();
        var options = filter.Options ?? Array.Empty<string>();
        var invalid = value.FieldId != filter.FieldId
            || values.Count > 20
            || values.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 100)
            || values.Distinct(StringComparer.Ordinal).Count() != values.Count;

        if (filter.Type == "date_range")
        {
            invalid |= values.Count > 0
                || string.IsNullOrWhiteSpace(value.Start)
                || string.IsNullOrWhiteSpace(value.End)
                || !DateOnly.TryParse(value.Start, out _)
                || !DateOnly.TryParse(value.End, out _);
        }
        else
        {
            invalid |= !string.IsNullOrWhiteSpace(value.Start)
                || !string.IsNullOrWhiteSpace(value.End)
                || values.Count == 0
                || (filter.Type is "single_select" or "record_status") && values.Count != 1
                || values.Any(item => !options.Contains(item, StringComparer.Ordinal));
        }

        if (invalid) errors.Add(new("config.filters.defaultValue", "dashboard.filter.default_invalid", "Dashboard filter default does not match its field, type, and bounded options."));
    }

    private static bool IsSecretKey(string key)
    {
        var normalized = key.ToLowerInvariant();
        return normalized.Contains("secret") || normalized.Contains("password") || normalized.Contains("credential")
            || normalized.Contains("connection") || normalized.Contains("token") || normalized.Contains("path") || normalized.Contains("file");
    }

    private static bool IsSafeSetting(object? value) => value is null or string or bool or byte or short or int or long or float or double or decimal
        || value is System.Text.Json.JsonElement element && element.ValueKind is System.Text.Json.JsonValueKind.String
            or System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False or System.Text.Json.JsonValueKind.Null;

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
