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
            ValidateWidget(widget, sources, errors);
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

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
