using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public sealed class DashboardDefinitionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly DashboardRecycleBinOptions recycleBinOptions;

    public DashboardDefinitionService(OpenBusinessPlatformDbContext dbContext, IOptions<DashboardRecycleBinOptions> recycleBinOptions)
    {
        this.dbContext = dbContext;
        this.recycleBinOptions = recycleBinOptions.Value;
    }

    public async Task<IReadOnlyCollection<DashboardSummaryDto>> ListAsync(DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var dashboards = await dbContext.Dashboards
            .AsNoTracking()
            .Where(dashboard => !dashboard.IsDeleted)
            .ToArrayAsync(cancellationToken);

        if (accessContext.CanManageDashboards)
        {
            return dashboards.OrderByDescending(dashboard => DashboardDefinitionAccess.ResolveSettings(dashboard).IsDefault)
                .ThenByDescending(dashboard => dashboard.UpdatedAt ?? dashboard.CreatedAt)
                .ThenBy(dashboard => dashboard.Name)
                .Select(ToSummaryDto)
                .ToArray();
        }

        return dashboards.Select(dashboard => new { Dashboard = dashboard, Snapshot = ResolvePublishedSnapshot(dashboard) })
            .Where(item => item.Snapshot is not null && CanViewPublished(item.Dashboard, item.Snapshot, accessContext))
            .Select(item => ToSummaryDto(item.Dashboard, item.Snapshot!))
            .OrderByDescending(item => item.IsDefault)
            .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .ThenBy(item => item.Name)
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

        if (accessContext.CanManageDashboards)
        {
            return ToDetailDto(dashboard);
        }

        var snapshot = ResolvePublishedSnapshot(dashboard);
        if (snapshot is null || !CanViewPublished(dashboard, snapshot, accessContext))
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }

        return ToDetailDto(dashboard, snapshot);
    }

    public async Task<DashboardDetailDto> GetBySlugAsync(string slug, DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var normalizedSlug = DashboardSlugs.Normalize(slug);
        var dashboard = await dbContext.Dashboards.AsNoTracking()
            .FirstOrDefaultAsync(candidate => !candidate.IsDeleted && candidate.Status == DashboardPublicationStatuses.Published
                && (candidate.PublishedSlug == normalizedSlug || candidate.PublishedSlug == null && candidate.Slug == normalizedSlug), cancellationToken);
        var snapshot = dashboard is null ? null : ResolvePublishedSnapshot(dashboard);
        if (dashboard is null || snapshot is null || !CanViewPublished(dashboard, snapshot, accessContext))
        {
            throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
        }
        return ToDetailDto(dashboard, snapshot);
    }

    public async Task<IReadOnlyCollection<DashboardNavigationItemDto>> ListNavigationAsync(DashboardAccessContext accessContext, CancellationToken cancellationToken)
    {
        var dashboards = await dbContext.Dashboards.AsNoTracking()
            .Where(item => !item.IsDeleted && item.Status == DashboardPublicationStatuses.Published
                && (item.PublishedSnapshotJson != null && item.PublishedShowInNavigation && item.PublishedSlug != null
                    || item.PublishedSnapshotJson == null && item.ShowInNavigation && item.Slug != null))
            .ToArrayAsync(cancellationToken);
        return dashboards.Select(item => new { Dashboard = item, Snapshot = ResolvePublishedSnapshot(item) })
            .Where(item => item.Snapshot is not null && CanViewPublished(item.Dashboard, item.Snapshot, accessContext))
            .OrderBy(item => item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.MenuOrder : item.Dashboard.PublishedMenuOrder)
            .ThenBy(item => item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.MenuLabel ?? item.Dashboard.Name : item.Dashboard.PublishedMenuLabel ?? item.Snapshot!.Name)
            .Select(item => new DashboardNavigationItemDto(item.Dashboard.Id,
                item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.Slug! : item.Dashboard.PublishedSlug!,
                item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.MenuLabel ?? item.Dashboard.Name : item.Dashboard.PublishedMenuLabel ?? item.Snapshot!.Name,
                item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.MenuIcon : item.Dashboard.PublishedMenuIcon,
                item.Dashboard.PublishedSnapshotJson is null ? item.Dashboard.MenuOrder : item.Dashboard.PublishedMenuOrder)).ToArray();
    }

    public async Task<DashboardDetailDto> CreateAsync(CreateDashboardRequest request, Guid? createdById, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        var settings = ValidateSettings(request.Settings);
        await ValidateSharingSubjectsAsync(settings, cancellationToken);
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
        await CreateRevisionAsync(dashboard, "created", createdById, cancellationToken);
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
        await ValidateSharingSubjectsAsync(settings, cancellationToken);
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
        await CreateRevisionAsync(dashboard, "saved", updatedById, cancellationToken);
        await SaveWithConflictAsync(cancellationToken);

        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> PublishAsync(Guid dashboardId, DashboardPublicationMutationRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        var config = Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new(1, Array.Empty<SavedDashboardWidgetDefinition>());
        var layout = Deserialize<SavedDashboardLayoutDefinition>(dashboard.LayoutJson) ?? new(1, Array.Empty<SavedDashboardWidgetLayoutDefinition>());
        await ValidateSharingSubjectsAsync(DashboardDefinitionAccess.ResolveSettings(dashboard), cancellationToken);
        await ValidateRequestAsync(NormalizeName(dashboard.Name), config, layout, cancellationToken);
        await ValidatePublicationAsync(ToPublication(dashboard) with { Status = DashboardPublicationStatuses.Published }, dashboard.Id, cancellationToken, requirePublishable: true, config);
        var snapshot = CreateSnapshot(dashboard) with { Publication = ToPublication(dashboard) with { Status = DashboardPublicationStatuses.Published } };
        dashboard.Status = DashboardPublicationStatuses.Published;
        dashboard.PublishedSnapshotJson = Serialize(snapshot);
        dashboard.PublishedSlug = snapshot.Publication.Slug;
        dashboard.PublishedShowInNavigation = snapshot.Publication.ShowInNavigation;
        dashboard.PublishedMenuLabel = snapshot.Publication.MenuLabel;
        dashboard.PublishedMenuIcon = snapshot.Publication.MenuIcon;
        dashboard.PublishedMenuOrder = snapshot.Publication.MenuOrder;
        dashboard.PublishedViewPermission = snapshot.Publication.ViewPermission;
        dashboard.PublishedAt = DateTimeOffset.UtcNow;
        dashboard.PublishedById = userId;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_published", userId);
        await CreateRevisionAsync(dashboard, "published", userId, cancellationToken, snapshot);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> UnpublishAsync(Guid dashboardId, DashboardPublicationMutationRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        dashboard.Status = DashboardPublicationStatuses.Draft;
        dashboard.ShowInNavigation = false;
        dashboard.PublishedSlug = null;
        dashboard.PublishedShowInNavigation = false;
        dashboard.PublishedMenuLabel = null;
        dashboard.PublishedMenuIcon = null;
        dashboard.PublishedMenuOrder = 0;
        dashboard.PublishedViewPermission = null;
        dashboard.PublishedAt = null;
        dashboard.PublishedById = null;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_unpublished", userId);
        AddAudit("Dashboard", dashboard.Id, "dashboard_navigation_changed", userId);
        await CreateRevisionAsync(dashboard, "unpublished", userId, cancellationToken);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    public async Task DeleteAsync(Guid dashboardId, DashboardPublicationMutationRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        dashboard.IsDeleted = true;
        dashboard.DeletedAt = DateTimeOffset.UtcNow;
        dashboard.DeletedById = userId;
        dashboard.Status = DashboardPublicationStatuses.Draft;
        var settings = DashboardDefinitionAccess.ResolveSettings(dashboard);
        dashboard.ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(settings with { IsDefault = false });
        dashboard.Slug = null;
        dashboard.ShowInNavigation = false;
        dashboard.PublishedSlug = null;
        dashboard.PublishedShowInNavigation = false;
        dashboard.PublishedMenuLabel = null;
        dashboard.PublishedMenuIcon = null;
        dashboard.PublishedMenuOrder = 0;
        dashboard.PublishedViewPermission = null;
        dashboard.PublishedSnapshotJson = null;
        dashboard.PublishedAt = null;
        dashboard.PublishedById = null;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_deleted", userId);
        await SaveWithConflictAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ArchivedDashboardDto>> ListArchivedAsync(CancellationToken cancellationToken)
    {
        var dashboards = await dbContext.Dashboards.AsNoTracking()
            .Where(dashboard => dashboard.IsDeleted)
            .OrderByDescending(dashboard => dashboard.DeletedAt)
            .ThenBy(dashboard => dashboard.Name)
            .ToArrayAsync(cancellationToken);
        var actorIds = dashboards.Where(item => item.DeletedById.HasValue).Select(item => item.DeletedById!.Value).Distinct().ToArray();
        var actorNames = await dbContext.Users.AsNoTracking().Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var minimumAge = TimeSpan.FromDays(recycleBinOptions.GetBoundedMinimumAgeDays());
        return dashboards.Select(dashboard =>
        {
            var archivedAt = dashboard.DeletedAt ?? dashboard.UpdatedAt ?? dashboard.CreatedAt;
            var availableAt = archivedAt.Add(minimumAge);
            var config = Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new(1, Array.Empty<SavedDashboardWidgetDefinition>());
            return new ArchivedDashboardDto(dashboard.Id, dashboard.Name, dashboard.Description, config.Widgets.Count, archivedAt,
                dashboard.DeletedById, dashboard.DeletedById.HasValue && actorNames.TryGetValue(dashboard.DeletedById.Value, out var actorName) ? actorName : null,
                dashboard.ConcurrencyStamp, availableAt, now >= availableAt);
        }).ToArray();
    }

    public async Task<DashboardDetailDto> RestoreArchivedAsync(Guid dashboardId, DashboardPublicationMutationRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindArchivedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        dashboard.IsDeleted = false;
        dashboard.DeletedAt = null;
        dashboard.DeletedById = null;
        dashboard.Status = DashboardPublicationStatuses.Draft;
        dashboard.Slug = null;
        dashboard.ShowInNavigation = false;
        dashboard.UpdatedById = userId;
        AddAudit("Dashboard", dashboard.Id, "dashboard_archive_restored", userId);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    public async Task PermanentlyDeleteAsync(Guid dashboardId, DashboardPermanentDeleteRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindArchivedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        if (!string.Equals(dashboard.Name, request.ConfirmationName, StringComparison.Ordinal))
            throw new DashboardDefinitionException(StatusCodes.Status400BadRequest, "Dashboard name confirmation does not match.",
                new[] { new DashboardValidationError("confirmationName", "dashboard.delete.confirmation_mismatch", "Type the exact dashboard name to permanently delete it.") });
        var archivedAt = dashboard.DeletedAt ?? dashboard.UpdatedAt ?? dashboard.CreatedAt;
        var availableAt = archivedAt.AddDays(recycleBinOptions.GetBoundedMinimumAgeDays());
        if (DateTimeOffset.UtcNow < availableAt)
            throw new DashboardDefinitionException(StatusCodes.Status409Conflict, $"This dashboard can be permanently deleted after {availableAt:O}.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        AddAudit("Dashboard", dashboard.Id, "dashboard_permanently_deleted", userId, Serialize(new
        {
            dashboard.Name,
            ArchivedAt = archivedAt,
            PermanentlyDeletedAt = DateTimeOffset.UtcNow
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        var deleted = await dbContext.Dashboards
            .Where(item => item.Id == dashboard.Id && item.IsDeleted && item.ConcurrencyStamp == request.ConcurrencyStamp)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted != 1)
            throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard changed before permanent deletion. Refresh and try again.");
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardRevisionSummaryDto>> ListRevisionsAsync(Guid dashboardId, CancellationToken cancellationToken)
    {
        await FindManagedAsync(dashboardId, cancellationToken);
        return await dbContext.DashboardRevisions.AsNoTracking()
            .Where(revision => revision.DashboardId == dashboardId)
            .OrderByDescending(revision => revision.RevisionNumber)
            .Take(50)
            .Select(revision => new DashboardRevisionSummaryDto(revision.Id, revision.RevisionNumber, revision.Reason, revision.CreatedAt, revision.CreatedById, revision.Reason == "published"))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<DashboardPublishedComparisonDto> GetPublishedComparisonAsync(Guid dashboardId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        var snapshot = ResolvePublishedSnapshot(dashboard);
        return new DashboardPublishedComparisonDto(snapshot is not null, snapshot, dashboard.PublishedAt, dashboard.PublishedById);
    }

    public async Task<DashboardSharingSettingsDto> GetSharingAsync(Guid dashboardId, CancellationToken cancellationToken)
    {
        var settings = DashboardDefinitionAccess.ResolveSettings(await FindManagedAsync(dashboardId, cancellationToken));
        return new DashboardSharingSettingsDto(
            settings.ViewerUserIds ?? Array.Empty<Guid>(),
            settings.ViewerRoleIds ?? Array.Empty<Guid>(),
            settings.ViewerGroupIds ?? Array.Empty<Guid>());
    }

    public async Task<DashboardSharingOptionsDto> GetSharingOptionsAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive && user.WorkspaceMemberships.Any(membership =>
                membership.WorkspaceId == dbContext.ActiveWorkspaceId && membership.Status == WorkspaceMembershipStatuses.Active))
            .OrderBy(user => user.Name)
            .Select(user => new DashboardSharingOptionDto(user.Id, user.Name, user.Email))
            .ToArrayAsync(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking().Where(role => role.IsActive).OrderBy(role => role.Name)
            .Select(role => new DashboardSharingOptionDto(role.Id, role.Name, role.Description)).ToArrayAsync(cancellationToken);
        var groups = await dbContext.Groups.AsNoTracking().Where(group => group.IsActive).OrderBy(group => group.Name)
            .Select(group => new DashboardSharingOptionDto(group.Id, group.Name, group.Description)).ToArrayAsync(cancellationToken);
        return new DashboardSharingOptionsDto(users, roles, groups);
    }

    public async Task<DashboardDetailDto> RestoreRevisionAsync(Guid dashboardId, Guid revisionId, DashboardRevisionRestoreRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var dashboard = await FindManagedAsync(dashboardId, cancellationToken);
        EnsureConcurrencyStamp(dashboard, request.ConcurrencyStamp);
        var revision = await dbContext.DashboardRevisions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == revisionId && item.DashboardId == dashboardId, cancellationToken)
            ?? throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard revision was not found.");
        var snapshot = Deserialize<DashboardRevisionSnapshotDefinition>(revision.SnapshotJson)
            ?? throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard revision snapshot is invalid.");
        var name = NormalizeName(snapshot.Name);
        var settings = ValidateSettings(snapshot.Settings);
        await ValidateSharingSubjectsAsync(settings, cancellationToken);
        await ValidateRequestAsync(name, snapshot.Config, snapshot.Layout, cancellationToken);
        var publication = await ValidatePublicationAsync(snapshot.Publication, dashboard.Id, cancellationToken,
            dashboard.Status == DashboardPublicationStatuses.Published, snapshot.Config);

        dashboard.Name = name;
        dashboard.Description = NormalizeOptionalText(snapshot.Description);
        dashboard.ConfigJson = Serialize(snapshot.Config);
        dashboard.LayoutJson = Serialize(snapshot.Layout);
        dashboard.ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(settings);
        dashboard.Slug = publication.Slug;
        dashboard.ShowInNavigation = publication.ShowInNavigation;
        dashboard.MenuLabel = publication.MenuLabel;
        dashboard.MenuIcon = publication.MenuIcon;
        dashboard.MenuOrder = publication.MenuOrder;
        dashboard.ViewPermission = publication.ViewPermission;
        dashboard.UpdatedById = userId;
        await ClearDefaultDashboardsIfNeededAsync(dashboard.Id, settings, userId, cancellationToken);
        AddAudit("Dashboard", dashboard.Id, "dashboard_revision_restored", userId);
        await CreateRevisionAsync(dashboard, "restored", userId, cancellationToken);
        await SaveWithConflictAsync(cancellationToken);
        return ToDetailDto(dashboard);
    }

    private static void EnsureConcurrencyStamp(DashboardDefinition dashboard, string concurrencyStamp)
    {
        if (!string.Equals(dashboard.ConcurrencyStamp, concurrencyStamp, StringComparison.Ordinal))
        {
            throw new DashboardDefinitionException(StatusCodes.Status409Conflict, "Dashboard was updated by someone else. Refresh and try again.");
        }
    }

    private async Task<DashboardDefinition> FindManagedAsync(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await dbContext.Dashboards.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        return dashboard ?? throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Dashboard was not found.");
    }

    private async Task<DashboardDefinition> FindArchivedAsync(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await dbContext.Dashboards.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken);
        return dashboard ?? throw new DashboardDefinitionException(StatusCodes.Status404NotFound, "Archived dashboard was not found.");
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

    private async Task ValidateSharingSubjectsAsync(DashboardSettingsDefinition settings, CancellationToken cancellationToken)
    {
        var userIds = settings.ViewerUserIds ?? Array.Empty<Guid>();
        var roleIds = settings.ViewerRoleIds ?? Array.Empty<Guid>();
        var groupIds = settings.ViewerGroupIds ?? Array.Empty<Guid>();
        var errors = new List<DashboardValidationError>();

        if (userIds.Count > 0)
        {
            var valid = await dbContext.Users.AsNoTracking().Where(user => userIds.Contains(user.Id) && user.IsActive
                && user.WorkspaceMemberships.Any(membership => membership.WorkspaceId == dbContext.ActiveWorkspaceId
                    && membership.Status == WorkspaceMembershipStatuses.Active)).Select(user => user.Id).ToArrayAsync(cancellationToken);
            if (valid.Length != userIds.Count) errors.Add(new("settings.viewerUserIds", "dashboard.sharing.user_invalid", "One or more selected users are inactive or outside this workspace."));
        }
        if (roleIds.Count > 0 && await dbContext.Roles.AsNoTracking().CountAsync(role => roleIds.Contains(role.Id) && role.IsActive, cancellationToken) != roleIds.Count)
            errors.Add(new("settings.viewerRoleIds", "dashboard.sharing.role_invalid", "One or more selected roles are inactive or unavailable."));
        if (groupIds.Count > 0 && await dbContext.Groups.AsNoTracking().CountAsync(group => groupIds.Contains(group.Id) && group.IsActive, cancellationToken) != groupIds.Count)
            errors.Add(new("settings.viewerGroupIds", "dashboard.sharing.group_invalid", "One or more selected groups are inactive or unavailable."));

        if (errors.Count > 0) throw new DashboardDefinitionException(StatusCodes.Status400BadRequest, "Dashboard sharing settings are invalid.", errors);
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
        if (slug.Length > 0 && await dbContext.Dashboards.AsNoTracking().AnyAsync(item => item.Id != dashboardId && (item.Slug == slug || item.PublishedSlug == slug), cancellationToken))
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
            dashboard.PublishedAt,
            dashboard.PublishedById,
            dashboard.ConcurrencyStamp,
            dashboard.CreatedAt,
            dashboard.CreatedById,
            dashboard.UpdatedAt,
            dashboard.UpdatedById);
    }

    private static DashboardSummaryDto ToSummaryDto(DashboardDefinition dashboard, DashboardRevisionSnapshotDefinition snapshot)
    {
        return new DashboardSummaryDto(
            dashboard.Id,
            snapshot.Name,
            snapshot.Description,
            snapshot.Config.Widgets.Count,
            snapshot.Settings.Visibility,
            snapshot.Settings.IsDefault,
            snapshot.Publication with { Status = DashboardPublicationStatuses.Published },
            dashboard.PublishedAt,
            dashboard.PublishedById,
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
            dashboard.PublishedAt,
            dashboard.PublishedById,
            dashboard.ConcurrencyStamp,
            dashboard.CreatedAt,
            dashboard.CreatedById,
            dashboard.UpdatedAt,
            dashboard.UpdatedById);
    }

    private static DashboardDetailDto ToDetailDto(DashboardDefinition dashboard, DashboardRevisionSnapshotDefinition snapshot)
    {
        return new DashboardDetailDto(
            dashboard.Id,
            snapshot.Name,
            snapshot.Description,
            snapshot.Config,
            snapshot.Layout,
            snapshot.Settings.Visibility,
            snapshot.Settings.IsDefault,
            snapshot.Publication with { Status = DashboardPublicationStatuses.Published },
            dashboard.PublishedAt,
            dashboard.PublishedById,
            dashboard.ConcurrencyStamp,
            dashboard.CreatedAt,
            dashboard.CreatedById,
            dashboard.UpdatedAt,
            dashboard.UpdatedById);
    }

    private DashboardRevisionSnapshotDefinition CreateSnapshot(DashboardDefinition dashboard) => new(
        dashboard.Name,
        dashboard.Description,
        Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new SavedDashboardConfigDefinition(1, Array.Empty<SavedDashboardWidgetDefinition>()),
        Deserialize<SavedDashboardLayoutDefinition>(dashboard.LayoutJson) ?? new SavedDashboardLayoutDefinition(1, Array.Empty<SavedDashboardWidgetLayoutDefinition>()),
        DashboardDefinitionAccess.ResolveSettings(dashboard),
        ToPublication(dashboard));

    private static DashboardRevisionSnapshotDefinition? ResolvePublishedSnapshot(DashboardDefinition dashboard)
    {
        if (dashboard.PublishedSnapshotJson is not null)
        {
            return Deserialize<DashboardRevisionSnapshotDefinition>(dashboard.PublishedSnapshotJson);
        }
        if (dashboard.Status != DashboardPublicationStatuses.Published) return null;
        return new DashboardRevisionSnapshotDefinition(
            dashboard.Name,
            dashboard.Description,
            Deserialize<SavedDashboardConfigDefinition>(dashboard.ConfigJson) ?? new SavedDashboardConfigDefinition(1, Array.Empty<SavedDashboardWidgetDefinition>()),
            Deserialize<SavedDashboardLayoutDefinition>(dashboard.LayoutJson) ?? new SavedDashboardLayoutDefinition(1, Array.Empty<SavedDashboardWidgetLayoutDefinition>()),
            DashboardDefinitionAccess.ResolveSettings(dashboard),
            ToPublication(dashboard));
    }

    private static bool CanViewPublished(DashboardDefinition dashboard, DashboardRevisionSnapshotDefinition snapshot, DashboardAccessContext accessContext)
    {
        if (accessContext.CanManageDashboards) return true;
        if (!string.IsNullOrWhiteSpace(snapshot.Publication.ViewPermission)
            && !(accessContext.Permissions?.Contains(snapshot.Publication.ViewPermission) ?? false)) return false;
        if (snapshot.Settings.Visibility == DashboardVisibilityModes.Workspace)
        {
            var userIds = snapshot.Settings.ViewerUserIds ?? Array.Empty<Guid>();
            var roleIds = snapshot.Settings.ViewerRoleIds ?? Array.Empty<Guid>();
            var groupIds = snapshot.Settings.ViewerGroupIds ?? Array.Empty<Guid>();
            if (userIds.Count == 0 && roleIds.Count == 0 && groupIds.Count == 0) return true;
            return accessContext.UserId.HasValue && userIds.Contains(accessContext.UserId.Value)
                || roleIds.Any(id => accessContext.RoleIds?.Contains(id) ?? false)
                || groupIds.Any(id => accessContext.GroupIds?.Contains(id) ?? false);
        }
        return accessContext.UserId.HasValue && dashboard.CreatedById == accessContext.UserId.Value;
    }

    private async Task CreateRevisionAsync(DashboardDefinition dashboard, string reason, Guid? userId, CancellationToken cancellationToken, DashboardRevisionSnapshotDefinition? snapshot = null)
    {
        var revisions = await dbContext.DashboardRevisions
            .Where(item => item.DashboardId == dashboard.Id)
            .OrderByDescending(item => item.RevisionNumber)
            .ToArrayAsync(cancellationToken);
        dbContext.DashboardRevisions.Add(new DashboardRevision
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboard.Id,
            RevisionNumber = (revisions.FirstOrDefault()?.RevisionNumber ?? 0) + 1,
            Reason = reason,
            SnapshotJson = Serialize(snapshot ?? CreateSnapshot(dashboard)),
            CreatedById = userId
        });
        if (revisions.Length >= 50) dbContext.DashboardRevisions.RemoveRange(revisions.Skip(49));
    }

    private static DashboardPublicationSettingsDefinition ToPublication(DashboardDefinition dashboard) => new(
        dashboard.Status, dashboard.Slug, dashboard.ShowInNavigation, dashboard.MenuLabel, dashboard.MenuIcon, dashboard.MenuOrder, dashboard.ViewPermission);

    private void AddAudit(string entityType, Guid entityId, string action, Guid? userId = null, JsonDocument? metadata = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            MetadataJson = metadata
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
