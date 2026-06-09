using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public static class DashboardVisibilityModes
{
    public const string Workspace = "workspace";
    public const string Private = "private";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Workspace,
        Private
    };
}

public sealed record DashboardSettingsDefinition(
    string Visibility = DashboardVisibilityModes.Workspace,
    bool IsDefault = false);

public sealed record DashboardAccessContext(Guid? UserId, bool CanManageDashboards);

public static class DashboardDefinitionAccess
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DashboardSettingsDefinition NormalizeSettings(DashboardSettingsDefinition? settings)
    {
        var visibility = settings?.Visibility?.Trim().ToLowerInvariant();

        return new DashboardSettingsDefinition(
            string.IsNullOrWhiteSpace(visibility) ? DashboardVisibilityModes.Workspace : visibility,
            settings?.IsDefault ?? false);
    }

    public static DashboardValidationResult ValidateSettings(DashboardSettingsDefinition? settings)
    {
        var normalized = NormalizeSettings(settings);
        var errors = new List<DashboardValidationError>();

        if (!DashboardVisibilityModes.Supported.Contains(normalized.Visibility))
        {
            errors.Add(new DashboardValidationError(
                "settings.visibility",
                "dashboard.visibility.unsupported",
                "Dashboard visibility must be workspace or private."));
        }

        if (string.Equals(normalized.Visibility, DashboardVisibilityModes.Private, StringComparison.Ordinal) && normalized.IsDefault)
        {
            errors.Add(new DashboardValidationError(
                "settings.isDefault",
                "dashboard.default.private_not_supported",
                "Only workspace-visible dashboards can be the shared default."));
        }

        return new DashboardValidationResult(errors);
    }

    public static DashboardSettingsDefinition ResolveSettings(DashboardDefinition dashboard)
    {
        var stored = DeserializeSettings(dashboard.ExtraPropertiesJson);
        var normalized = NormalizeSettings(stored);

        if (!DashboardVisibilityModes.Supported.Contains(normalized.Visibility))
        {
            return new DashboardSettingsDefinition();
        }

        if (string.Equals(normalized.Visibility, DashboardVisibilityModes.Private, StringComparison.Ordinal) && normalized.IsDefault)
        {
            return normalized with { IsDefault = false };
        }

        return normalized;
    }

    public static bool CanView(DashboardDefinition dashboard, DashboardAccessContext accessContext)
    {
        if (accessContext.CanManageDashboards)
        {
            return true;
        }

        var settings = ResolveSettings(dashboard);

        if (string.Equals(settings.Visibility, DashboardVisibilityModes.Workspace, StringComparison.Ordinal))
        {
            return true;
        }

        return accessContext.UserId.HasValue && dashboard.CreatedById == accessContext.UserId.Value;
    }

    public static JsonDocument SerializeSettings(DashboardSettingsDefinition settings)
    {
        return JsonSerializer.SerializeToDocument(NormalizeSettings(settings), JsonOptions);
    }

    private static DashboardSettingsDefinition? DeserializeSettings(JsonDocument? jsonDocument)
    {
        if (jsonDocument is null)
        {
            return null;
        }

        try
        {
            return jsonDocument.RootElement.Deserialize<DashboardSettingsDefinition>(JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
