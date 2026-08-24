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
    bool IsDefault = false,
    IReadOnlyCollection<Guid>? ViewerUserIds = null,
    IReadOnlyCollection<Guid>? ViewerRoleIds = null,
    IReadOnlyCollection<Guid>? ViewerGroupIds = null);

public sealed record DashboardAccessContext(
    Guid? UserId,
    bool CanManageDashboards,
    IReadOnlySet<string>? Permissions = null,
    IReadOnlySet<Guid>? RoleIds = null,
    IReadOnlySet<Guid>? GroupIds = null);

public static class DashboardPublicationStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
}

public static class DashboardMenuIcons
{
    public static IReadOnlySet<string> Approved { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "layout-dashboard", "chart-column", "chart-line", "factory", "landmark", "briefcase-business", "activity"
    };
}

public static class DashboardSlugs
{
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal) { "new", "builder", "settings" };

    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool IsValid(string value) =>
        value.Length is >= 2 and <= 100 &&
        !Reserved.Contains(value) &&
        System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$");
}

public static class DashboardDefinitionAccess
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DashboardSettingsDefinition NormalizeSettings(DashboardSettingsDefinition? settings)
    {
        var visibility = settings?.Visibility?.Trim().ToLowerInvariant();

        return new DashboardSettingsDefinition(
            string.IsNullOrWhiteSpace(visibility) ? DashboardVisibilityModes.Workspace : visibility,
            settings?.IsDefault ?? false,
            NormalizeIds(settings?.ViewerUserIds),
            NormalizeIds(settings?.ViewerRoleIds),
            NormalizeIds(settings?.ViewerGroupIds));
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

        var sharingCount = normalized.ViewerUserIds!.Count + normalized.ViewerRoleIds!.Count + normalized.ViewerGroupIds!.Count;
        if (sharingCount > 100)
        {
            errors.Add(new DashboardValidationError("settings.sharing", "dashboard.sharing.limit", "Choose at most 100 users, roles, and groups combined."));
        }
        if (string.Equals(normalized.Visibility, DashboardVisibilityModes.Private, StringComparison.Ordinal) && sharingCount > 0)
        {
            errors.Add(new DashboardValidationError("settings.sharing", "dashboard.sharing.private_not_supported", "Private dashboards cannot include additional viewers."));
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

        if (!string.Equals(dashboard.Status, DashboardPublicationStatuses.Published, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(dashboard.ViewPermission)
            && !(accessContext.Permissions?.Contains(dashboard.ViewPermission) ?? false))
        {
            return false;
        }

        var settings = ResolveSettings(dashboard);

        if (string.Equals(settings.Visibility, DashboardVisibilityModes.Workspace, StringComparison.Ordinal))
        {
            var userIds = settings.ViewerUserIds ?? Array.Empty<Guid>();
            var roleIds = settings.ViewerRoleIds ?? Array.Empty<Guid>();
            var groupIds = settings.ViewerGroupIds ?? Array.Empty<Guid>();
            if (userIds.Count == 0 && roleIds.Count == 0 && groupIds.Count == 0) return true;
            return accessContext.UserId.HasValue && userIds.Contains(accessContext.UserId.Value)
                || roleIds.Any(id => accessContext.RoleIds?.Contains(id) ?? false)
                || groupIds.Any(id => accessContext.GroupIds?.Contains(id) ?? false);
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

    private static IReadOnlyCollection<Guid> NormalizeIds(IReadOnlyCollection<Guid>? ids) =>
        (ids ?? Array.Empty<Guid>()).Where(id => id != Guid.Empty).Distinct().Take(101).ToArray();
}
