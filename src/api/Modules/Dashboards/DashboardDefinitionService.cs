using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public sealed class DashboardDefinitionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public DashboardDefinitionService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DashboardSummaryDto>> ListAsync(DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var dashboards = await dbContext.Dashboards
            .AsNoTracking()
            .Where(dashboard => !dashboard.IsDeleted)
            .ToArrayAsync(cancellationToken);

        return dashboards
            .Where(dashboard => DashboardDefinitionAccess.CanView(dashboard, accessContext))
            .OrderByDescending(dashboard => DashboardDefinitionAccess.ResolveSettings(dashboard).IsDefault)
            .ThenByDescending(dashboard => dashboard.UpdatedAt ?? dashboard.CreatedAt)
            .ThenBy(dashboard => dashboard.Name)
            .Select(ToSummaryDto)
            .ToArray();
    }

    public async Task<DashboardDetailDto> GetAsync(Guid dashboardId, DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var dashboard = await dbContext.Dashboards
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == dashboardId && !candidate.IsDeleted, cancellationToken);

        if (dashboard is null)
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }

        if (!DashboardDefinitionAccess.CanView(dashboard, accessContext))
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }

        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> GetBySlugAsync(string slug, DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var normalizedSlug = DashboardSlugs.Normalize(slug);
        var dashboard = await dbContext.Dashboards.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Slug == normalizedSlug && !candidate.IsDeleted, cancellationToken);
        if (dashboard is null || dashboard.Status != DashboardPublicationStatuses.Published || !DashboardDefinitionAccess.CanView(dashboard, accessContext))
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }
        return ToDetailDto(dashboard);
    }

    public async Task<IReadOnlyCollection<DashboardNavigationItemDto>> ListNavigationAsync(DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var dashboards = await dbContext.Dashboards.AsNoTracking()
            .Where(item => !item.IsDeleted && item.Status == DashboardPublicationStatuses.Published && item.ShowInNavigation && item.Slug != null)
            .ToArrayAsync(cancellationToken);
        return dashboards.Where(item => DashboardDefinitionAccess.CanView(item, accessContext))
            .OrderBy(item => item.MenuOrder).ThenBy(item => item.MenuLabel ?? item.Name)
            .Select(item => new DashboardNavigationItemDto(item.Id, item.Slug!, item.MenuLabel ?? item.Name, item.MenuIcon, item.MenuOrder)).ToArray();
    }

    public async Task<DashboardDetailDto> CreateAsync(CreateDashboardRequest request, Guid? createdById, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        var settings = ValidateSettings(request.Settings);
        await ValidateRequestAsync(name, request.Config, request.Layout, cancellationToken);
        var publication = await ValidatePublicationAsync(request.Publication, null, cancellationToken);

        var dashboard = new DashboardDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeOptionalText(request.Description),
            ConfigJson = Serialize(request.Config),
            LayoutJson = Serialize(request.Layout),
            ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(settings),
            Status = DashboardPublicationStatuses.Draft,
            Slug = publication.Slug,
            ShowInNavigation = publication.ShowInNavigation,
            MenuLabel = publication.MenuLabel,
            MenuIcon = publication.MenuIcon,
            MenuOrder = publication.MenuOrder,
            ViewPermission = publication.ViewPermission,
            CreatedById = createdById
        };

        dbContext.Dashboards.Add(dashboard);
        await ClearDefaultDashboardsIfNeededAsync(dashboard.Id, settings, createdById, cancellationToken);
        AddAudit("Dashboard", dashboard.Id, "dashboard_created", createdById);
        await SaveWithConflictAsync(cancellationToken);

        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> UpdateAsync(Guid dashboardId, UpdateDashboardRequest request, Guid? updatedById, CancellationToken cancellationToken)
    {
        var dashboard = await dbContext.Dashboards
            .FirstOrDefaultAsync(candidate => candidate.Id == dashboardId && !candidate.IsDeleted, cancellationToken);

        if (dashboard is null)
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }

        if (!string.Equals(dashboard.ConcurrencyStamp, request.ConcurrencyStamp, StringComparison.Ordinal))
        {
            throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard was updated by someone else. Refresh and try again.");
        }

        var name = NormalizeName(request.Name);
        var settings = request.Settings is null ? DashboardDefinitionAccess.ResolveSettings(dashboard) : ValidateSettings(request.Settings);
        await ValidateRequestAsync(name, request.Config, request.Layout, cancellationToken);
        var publication = await ValidatePublicationAsync(request.Publication ?? ToPublication(dashboard), dashboard.Id, cancellationToken,
            dashboard.Status == DashboardPublicationStatuses.Published, request.Config);
        var navigationChanged = dashboard.Slug != publication.Slug || dashboard.ShowInNavigation != publication.ShowInNavigation || dashboard.MenuLabel != publication.MenuLabel
            || dashboard.MenuIcon != publication.MenuIcon || dashboard.MenuOrder != publication.MenuOrder || dashboard.ViewPermission != publication.ViewPermission;

        dashboard.Name = name;
        dashboard.Description = NormalizeOptionalText(request.Description);
        dashboard.ConfigJson = Serialize(request.Config);
        dashboard.LayoutJson = Serialize(request.Layout);
        dashboard.ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(settings);
        dashboard.Slug = publication.Slug;
        dashboard.ShowInNavigation = publication.ShowInNavigation;
        dashboard.MenuLabel = publication.MenuLabel;
        dashboard.MenuIcon = publication.MenuIcon;
        dashboard.MenuOrder = publication.MenuOrder;
        dashboard.ViewPermission = publication.ViewPermission;
        dashboard.UpdatedById = updatedById;
        await ClearDefaultDashboardsIfNeededAsync(dashboard.Id, settings, updatedById, cancellationToken);
        AddAudit("Dashboard", dashboard.Id, "dashboard_updated", updatedById);
        if (navigationChanged) AddAudit("Dashboard", dashboard.Id, "dashboard_navigation_changed", updatedById);
        await SaveWithConflictAsync(cancellationToken);

        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> PublishAsync(Guid dashboardId, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        var config = Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new(1, Array.Empty<SavedDashboardWidgetDefinition>());
        var layout = Deserialize<SavedDashboardLayoutDefinition>(dashboard.LayoutJson) ?? new(1, Array.Empty<SavedDashboardWidgetLayoutDefinition>());
        await ValidateRequestAsync(NormalizeName(dashboard.Name), config, layout, cancellationToken);
        await ValidatePublicationAsync(ToPublication(dashboard) with { Status = DashboardPublicationStatuses.Published }, dashboard.Id, cancellationToken, requirePublishable: true, config);
        dashboard.Status = DashboardPublicationStatuses.Published;
        dashboard.PublishedAt = DateTimeOffset.UtcNow;
        dashboard.PublishedById = userId;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_published", userId);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> UnpublishAsync(Guid dashboardId, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        dashboard.Status = DashboardPublicationStatuses.Draft;
        dashboard.ShowInNavigation = false;
        dashboard.PublishedAt = null;
        dashboard.PublishedById = null;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_unpublished", userId);
        AddAudit("Dashboard", dashboard.Id, "dashboard_navigation_changed", userId);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    private async Task<DashboardDefinition> FindManagedAsync(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await dbContext.Dashboards.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        return dashboard ?? throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
    }

    private async Task SaveWithConflictAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard was updated by someone else. Refresh and try again."); }
        catch (DbUpdateException) { throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard publishing settings conflict with another saved dashboard."); }
    }

    private async Task ValidateRequestAsync(
        string name,
        SavedDashboardConfigDefinition config,
        SavedDashboardLayoutDefinition layout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DashboardDefinitionException(
                StatusCodes.Status400BadRequest,
                "Dashboard config is invalid.",
                new[] { new DashboardValidationError("name", "dashboard.name.required", "Dashboard name is required.") });
        }

        var sources = await LoadSourcesAsync(config, cancellationToken);
        var validation = DashboardDefinitionValidator.Validate(config, layout, sources);

        if (!validation.Valid)
        {
            throw new DashboardDefinitionException(StatusCodes.Status400BadRequest, "Dashboard config is invalid.", validation.Errors);
        }
    }

    private static DashboardSettingsDefinition ValidateSettings(DashboardSettingsDefinition? settings)
    {
        var validation = DashboardDefinitionAccess.ValidateSettings(settings);

        if (!validation.Valid)
        {
            throw new DashboardDefinitionException(StatusCodes.Status400BadRequest, "Dashboard settings are invalid.", validation.Errors);
        }

        return DashboardDefinitionAccess.NormalizeSettings(settings);
    }

    private async Task<DashboardPublicationSettingsDefinition> ValidatePublicationAsync(
        DashboardPublicationSettingsDefinition? input,
        Guid? dashboardId,
        CancellationToken cancellationToken,
        bool requirePublishable = false,
        SavedDashboardConfigDefinition? config = null)
    {
        var publication = input ?? new(DashboardPublicationStatuses.Draft, null, false, null, null, 0, null);
        var slug = DashboardSlugs.Normalize(publication.Slug);
        var errors = new List<DashboardValidationError>();
        if (slug.Length > 0 && !DashboardSlugs.IsValid(slug))
            errors.Add(new("publication.slug", "dashboard.slug.invalid", "Use 2-100 lowercase letters, numbers, or single hyphens; reserved values are not allowed."));
        if (slug.Length > 0 && await dbContext.Dashboards.AsNoTracking().AnyAsync(item => item.Id != dashboardId && item.Slug == slug, cancellationToken))
            errors.Add(new("publication.slug", "dashboard.slug.duplicate", "This dashboard URL slug is already in use."));
        if (publication.ShowInNavigation)
        {
            if (string.IsNullOrWhiteSpace(publication.MenuLabel)) errors.Add(new("publication.menuLabel", "dashboard.menu.label_required", "Menu label is required when navigation is enabled."));
            if (string.IsNullOrWhiteSpace(publication.MenuIcon) || !DashboardMenuIcons.Approved.Contains(publication.MenuIcon))
                errors.Add(new("publication.menuIcon", "dashboard.menu.icon_invalid", "Choose an approved dashboard menu icon."));
        }
        if (requirePublishable)
        {
            if (slug.Length == 0) errors.Add(new("publication.slug", "dashboard.slug.required", "A URL slug is required before publishing."));
            if ((config?.Sections?.Count ?? 0) == 0) errors.Add(new("config.sections", "dashboard.sections.required", "Add at least one section before publishing."));
        }
        if (errors.Count > 0) throw new DashboardDefinitionException(StatusCodes.Status400BadRequest, "Dashboard publishing settings are invalid.", errors);
        return publication with
        {
            Status = DashboardPublicationStatuses.Draft,
            Slug = slug.Length == 0 ? null : slug,
            MenuLabel = NormalizeOptionalText(publication.MenuLabel),
            MenuIcon = NormalizeOptionalText(publication.MenuIcon)?.ToLowerInvariant(),
            ViewPermission = NormalizeOptionalText(publication.ViewPermission)
        };
    }

    private async Task ClearDefaultDashboardsIfNeededAsync(
        Guid dashboardId,
        DashboardSettingsDefinition settings,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (!settings.IsDefault)
        {
            return;
        }

        var dashboards = await dbContext.Dashboards
            .Where(dashboard => dashboard.Id != dashboardId && !dashboard.IsDeleted)
            .ToArrayAsync(cancellationToken);

        foreach (var dashboard in dashboards)
        {
            var currentSettings = DashboardDefinitionAccess.ResolveSettings(dashboard);

            if (!currentSettings.IsDefault)
            {
                continue;
            }

            dashboard.ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(currentSettings with { IsDefault = false });
            dashboard.UpdatedById = userId;
            AddAudit("Dashboard", dashboard.Id, "dashboard_default_cleared", userId);
        }
    }

    private async Task<IReadOnlyCollection<DashboardSourceDefinition>> LoadSourcesAsync(
        SavedDashboardConfigDefinition config,
        CancellationToken cancellationToken)
    {
        var formIds = (config.Widgets ?? Array.Empty<SavedDashboardWidgetDefinition>())
            .Select(widget => widget.SourceFormId)
            .Where(formId => formId.HasValue)
            .Select(formId => formId!.Value)
            .Distinct()
            .ToArray();
        var forms = await dbContext.Forms
            .AsNoTracking()
            .Include(form => form.CurrentVersion)
            .Where(form => formIds.Contains(form.Id) && !form.IsDeleted)
            .ToArrayAsync(cancellationToken);
        var reports = await dbContext.Reports
            .AsNoTracking()
            .Where(report => formIds.Contains(report.FormId) && !report.IsDeleted)
            .ToArrayAsync(cancellationToken);

        return forms
            .Select(form => new
            {
                form.Id,
                Schema = ResolveSchema(form)
            })
            .Where(form => form.Schema is not null)
            .Select(form => new DashboardSourceDefinition(
                form.Id,
                form.Schema!,
                reports
                    .Where(report => report.FormId == form.Id)
                    .Select(report => new DashboardSourceReportDefinition(report.Id, report.Type))
                    .ToArray()))
            .ToArray();
    }

    private static DashboardSummaryDto ToSummaryDto(DashboardDefinition dashboard)
    {
        var config = Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson)
            ?? new SavedDashboardConfigDefinition(1, Array.Empty<SavedDashboardWidgetDefinition>());
        var settings = DashboardDefinitionAccess.ResolveSettings(dashboard);

        return new DashboardSummaryDto(
            dashboard.Id,
            dashboard.Name,
            dashboard.Description,
            config.Widgets.Count,
            settings.Visibility,
            settings.IsDefault,
            ToPublication(dashboard),
            dashboard.ConcurrencyStamp,
            dashboard.CreatedAt,
            dashboard.CreatedById,
            dashboard.UpdatedAt,
            dashboard.UpdatedById);
    }

    private static DashboardDetailDto ToDetailDto(DashboardDefinition dashboard)
    {
        var settings = DashboardDefinitionAccess.ResolveSettings(dashboard);

        return new DashboardDetailDto(
            dashboard.Id,
            dashboard.Name,
            dashboard.Description,
            Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new SavedDashboardConfigDefinition(1, Array.Empty<SavedDashboardWidgetDefinition>()),
            Deserialize<SavedDashboardLayoutDefinition>(dashboard.LayoutJson) ?? new SavedDashboardLayoutDefinition(1, Array.Empty<SavedDashboardWidgetLayoutDefinition>()),
            settings.Visibility,
            settings.IsDefault,
            ToPublication(dashboard),
            dashboard.ConcurrencyStamp,
            dashboard.CreatedAt,
            dashboard.CreatedById,
            dashboard.UpdatedAt,
            dashboard.UpdatedById);
    }

    private static DashboardPublicationSettingsDefinition ToPublication(DashboardDefinition dashboard) => new(
        dashboard.Status, dashboard.Slug, dashboard.ShowInNavigation, dashboard.MenuLabel, dashboard.MenuIcon, dashboard.MenuOrder, dashboard.ViewPermission);

    private void AddAudit(string entityType, Guid entityId, string action, Guid? userId = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId
        });
    }

    private static JsonDocument Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static T? Deserialize<T>(JsonDocument jsonDocument)
    {
        return jsonDocument.RootElement.Deserialize<T>(JsonOptions);
    }

    private static FormSchemaDefinition? ResolveSchema(FormDefinition form)
    {
        return DeserializeOptional<FormSchemaDefinition>(form.CurrentVersion?.SchemaJson)
            ?? DeserializeOptional<FormSchemaDefinition>(form.DraftSchemaJson);
    }

    private static T? DeserializeOptional<T>(JsonDocument? jsonDocument)
    {
        return jsonDocument is null ? default : jsonDocument.RootElement.Deserialize<T>(JsonOptions);
    }

    private static string NormalizeName(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
