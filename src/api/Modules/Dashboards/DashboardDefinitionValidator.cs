using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Dashboard;

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

        var widgets = config.Widgets ?? Array.Empty<SavedDashboardWidgetDefinition>();
        var layouts = layout.Widgets ?? Array.Empty<SavedDashboardWidgetLayoutDefinition>();
        var widgetIds = widgets.Select(widget => Normalize(widget.Id)).ToArray();
        var layoutIds = layouts.Select(item => Normalize(item.Id)).ToArray();
        var sections = config.Sections ?? Array.Empty<SavedDashboardSectionDefinition>();
        var sectionIds = sections.Select(section => Normalize(section.Id)).ToArray();

        foreach (var duplicate in sectionIds.Where(id => id.Length > 0).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add(new DashboardValidationError("config.sections", "dashboard.sections.duplicate_id", $"Section id '{duplicate.Key}' is duplicated."));
        }

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Id) || string.IsNullOrWhiteSpace(section.Title))
            {
                errors.Add(new DashboardValidationError("config.sections", "dashboard.section.invalid", "Each dashboard section needs an id and title."));
            }
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

        var source = sources.SingleOrDefault(candidate => candidate.FormId == widget.SourceFormId);

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
