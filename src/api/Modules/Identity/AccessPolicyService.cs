using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class AccessPolicyException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class AccessPolicyEvaluator(OpenBusinessPlatformDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> IsDeniedAsync(
        ClaimsPrincipal principal,
        AccessPolicyResource resource,
        CancellationToken cancellationToken)
    {
        var subject = await GetSubjectAsync(principal, cancellationToken);
        if (subject.IsBootstrap)
        {
            return false;
        }
        return (await GetMatchingPolicyIdsAsync(subject, resource, cancellationToken)).Count > 0;
    }

    public async Task<IReadOnlyCollection<Guid>> GetMatchingPolicyIdsAsync(
        AccessPolicySubject subject,
        AccessPolicyResource resource,
        CancellationToken cancellationToken)
    {
        if (subject.IsBootstrap)
        {
            return Array.Empty<Guid>();
        }
        var policies = await CandidatePolicies(resource).AsNoTracking().OrderByDescending(policy => policy.Priority).ToArrayAsync(cancellationToken);
        return policies
            .Where(policy => Matches(Deserialize(policy), subject, resource))
            .Select(policy => policy.Id)
            .ToArray();
    }

    public async Task<IQueryable<FormRecord>> ApplyRecordPoliciesAsync(
        ClaimsPrincipal principal,
        IQueryable<FormRecord> records,
        Guid formId,
        string action,
        CancellationToken cancellationToken)
    {
        var subject = await GetSubjectAsync(principal, cancellationToken);
        if (subject.IsBootstrap)
        {
            return records;
        }
        var policies = await CandidatePolicies(new AccessPolicyResource(AccessPolicyResourceTypes.Record, formId, action))
            .AsNoTracking()
            .OrderByDescending(policy => policy.Priority)
            .ToArrayAsync(cancellationToken);
        foreach (var policy in policies)
        {
            var conditions = Deserialize(policy);
            if (!MatchesSubject(conditions, subject))
            {
                continue;
            }
            records = ApplyRecordCondition(records, subject, conditions);
        }
        return records;
    }

    public async Task<AccessPolicySubject> GetSubjectAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) == BootstrapAdminUserDirectory.BootstrapAdminId)
        {
            return new(null, new HashSet<string>(), new HashSet<string>(), new HashSet<Guid>(), new HashSet<Guid>(), true);
        }
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? await GetSubjectAsync(userId, cancellationToken)
            : new(null, new HashSet<string>(), new HashSet<string>(), new HashSet<Guid>(), new HashSet<Guid>(), false);
    }

    public async Task<AccessPolicySubject> GetSubjectAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .Select(item => item.Role!.Name).ToArrayAsync(cancellationToken);
        var membershipRoles = await dbContext.WorkspaceMemberships.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == WorkspaceMembershipStatuses.Active)
            .Select(item => item.Role).ToArrayAsync(cancellationToken);
        var departments = await dbContext.UserDepartments.AsNoTracking()
            .Where(item => item.UserId == userId).Select(item => item.DepartmentId).ToArrayAsync(cancellationToken);
        var groups = await dbContext.UserGroups.AsNoTracking()
            .Where(item => item.UserId == userId).Select(item => item.GroupId).ToArrayAsync(cancellationToken);
        return new(
            userId,
            roles.ToHashSet(StringComparer.Ordinal),
            membershipRoles.ToHashSet(StringComparer.Ordinal),
            departments.ToHashSet(),
            groups.ToHashSet(),
            false);
    }

    public static bool Matches(AccessPolicyConditions conditions, AccessPolicySubject subject, AccessPolicyResource resource)
    {
        if (!MatchesSubject(conditions, subject))
        {
            return false;
        }
        if (resource.ResourceType != AccessPolicyResourceTypes.Record)
        {
            return conditions.RecordStatusAny is null or { Count: 0 } && conditions.RecordOwnerIsCurrentUser is null;
        }
        var statusMatches = conditions.RecordStatusAny is null or { Count: 0 }
            || conditions.RecordStatusAny.Contains(resource.RecordStatus ?? string.Empty, StringComparer.Ordinal);
        var ownerMatches = conditions.RecordOwnerIsCurrentUser is null
            || conditions.RecordOwnerIsCurrentUser == (subject.UserId is not null && resource.RecordOwnerUserId == subject.UserId);
        return statusMatches && ownerMatches;
    }

    public static IQueryable<FormRecord> ApplyRecordCondition(
        IQueryable<FormRecord> records,
        AccessPolicySubject subject,
        AccessPolicyConditions conditions)
    {
        var statuses = conditions.RecordStatusAny?.ToArray() ?? Array.Empty<string>();
        var ownerCondition = conditions.RecordOwnerIsCurrentUser;
        var currentUserId = subject.UserId;
        if (statuses.Length == 0 && ownerCondition is null)
        {
            return records.Where(record => false);
        }
        return records.Where(record =>
            (statuses.Length > 0 && !statuses.Contains(record.Status))
            || (ownerCondition == true && record.CreatedById != currentUserId)
            || (ownerCondition == false && record.CreatedById == currentUserId));
    }

    private static bool MatchesSubject(AccessPolicyConditions conditions, AccessPolicySubject subject)
    {
        return MatchesAny(conditions.RoleAny, subject.Roles)
            && MatchesAny(conditions.MembershipRoleAny, subject.MembershipRoles)
            && MatchesAny(conditions.DepartmentAny, subject.DepartmentIds)
            && MatchesAny(conditions.GroupAny, subject.GroupIds);
    }

    private IQueryable<AccessPolicy> CandidatePolicies(AccessPolicyResource resource)
    {
        return dbContext.AccessPolicies.Where(policy =>
            policy.IsEnabled
            && policy.ResourceType == resource.ResourceType
            && policy.Action == resource.Action
            && (policy.ResourceId == null || policy.ResourceId == resource.ResourceId));
    }

    private static bool MatchesAny<T>(IReadOnlyCollection<T>? required, IReadOnlySet<T> actual)
    {
        return required is null or { Count: 0 } || required.Any(actual.Contains);
    }

    internal static AccessPolicyConditions Deserialize(AccessPolicy policy)
    {
        return policy.ConditionsJson.RootElement.Deserialize<AccessPolicyConditions>(JsonOptions) ?? new AccessPolicyConditions();
    }
}

public sealed class AccessPolicyService(OpenBusinessPlatformDbContext dbContext, AccessPolicyEvaluator evaluator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<AccessPolicyDto>> ListAsync(CancellationToken cancellationToken)
    {
        var policies = await dbContext.AccessPolicies.AsNoTracking()
            .OrderByDescending(policy => policy.Priority).ThenBy(policy => policy.Name).ToArrayAsync(cancellationToken);
        return policies.Select(ToDto).ToArray();
    }

    public async Task<AccessPolicyDto> CreateAsync(SaveAccessPolicyRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var values = await ValidateAsync(request, cancellationToken);
        var policy = new AccessPolicy
        {
            Id = Guid.NewGuid(), Name = values.Name, Description = values.Description,
            ResourceType = values.ResourceType, ResourceId = values.ResourceId, Action = values.Action,
            ConditionsJson = JsonSerializer.SerializeToDocument(values.Conditions, JsonOptions),
            Priority = values.Priority, IsEnabled = values.IsEnabled, CreatedById = actorUserId
        };
        dbContext.AccessPolicies.Add(policy);
        AddAudit(policy.Id, "access_policy_created", actorUserId, policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(policy);
    }

    public async Task<AccessPolicyDto?> UpdateAsync(Guid id, SaveAccessPolicyRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var policy = await dbContext.AccessPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy is null) return null;
        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp) || policy.ConcurrencyStamp != request.ConcurrencyStamp.Trim())
            throw new DbUpdateConcurrencyException("The access policy changed. Refresh and try again.");
        var values = await ValidateAsync(request, cancellationToken);
        policy.Name = values.Name; policy.Description = values.Description; policy.ResourceType = values.ResourceType;
        policy.ResourceId = values.ResourceId; policy.Action = values.Action;
        policy.ConditionsJson = JsonSerializer.SerializeToDocument(values.Conditions, JsonOptions);
        policy.Priority = values.Priority; policy.IsEnabled = values.IsEnabled; policy.UpdatedById = actorUserId;
        AddAudit(policy.Id, "access_policy_updated", actorUserId, policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(policy);
    }

    public async Task<AccessPolicySimulationResponse> SimulateAsync(SimulateAccessPolicyRequest request, CancellationToken cancellationToken)
    {
        var active = await dbContext.WorkspaceMemberships.AnyAsync(item =>
            item.UserId == request.UserId && item.Status == WorkspaceMembershipStatuses.Active, cancellationToken);
        if (!active) throw new AccessPolicyException(StatusCodes.Status400BadRequest, "An active workspace member is required.");
        ValidateResource(request.ResourceType, request.ResourceId, request.Action);
        var subject = await evaluator.GetSubjectAsync(request.UserId, cancellationToken);
        var resource = new AccessPolicyResource(request.ResourceType, request.ResourceId, request.Action, request.RecordStatus, request.RecordOwnerUserId);
        var ids = await evaluator.GetMatchingPolicyIdsAsync(subject, resource, cancellationToken);
        return new(ids.Count > 0, ids);
    }

    private async Task<ValidatedPolicy> ValidateAsync(SaveAccessPolicyRequest request, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Name", 160);
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : Required(request.Description, "Description", 1000);
        var resourceType = request.ResourceType?.Trim().ToLowerInvariant() ?? string.Empty;
        var action = request.Action?.Trim() ?? string.Empty;
        ValidateResource(resourceType, request.ResourceId, action);
        if (request.Priority is < -1000 or > 1000) throw new AccessPolicyException(400, "Priority must be between -1000 and 1000.");
        var conditions = NormalizeConditions(request.Conditions ?? new AccessPolicyConditions());
        ValidateConditionBounds(conditions);
        if (resourceType != AccessPolicyResourceTypes.Record
            && (conditions.RecordStatusAny?.Count > 0 || conditions.RecordOwnerIsCurrentUser is not null))
            throw new AccessPolicyException(400, "Record conditions are valid only for record policies.");
        if (conditions.MembershipRoleAny?.Any(role => !WorkspaceMembershipRoles.Supported.Contains(role)) == true)
            throw new AccessPolicyException(400, "A membership role condition is invalid.");
        if (conditions.RoleAny is { Count: > 0 })
        {
            var existing = await dbContext.Roles.CountAsync(role => conditions.RoleAny.Contains(role.Name), cancellationToken);
            if (existing != conditions.RoleAny.Count) throw new AccessPolicyException(400, "One or more policy roles were not found.");
        }
        if (conditions.DepartmentAny is { Count: > 0 })
        {
            var existing = await dbContext.Departments.CountAsync(item => conditions.DepartmentAny.Contains(item.Id), cancellationToken);
            if (existing != conditions.DepartmentAny.Count) throw new AccessPolicyException(400, "One or more policy departments were not found.");
        }
        if (conditions.GroupAny is { Count: > 0 })
        {
            var existing = await dbContext.Groups.CountAsync(item => conditions.GroupAny.Contains(item.Id), cancellationToken);
            if (existing != conditions.GroupAny.Count) throw new AccessPolicyException(400, "One or more policy groups were not found.");
        }
        if (request.ResourceId is { } resourceId)
        {
            var exists = resourceType switch
            {
                AccessPolicyResourceTypes.Form or AccessPolicyResourceTypes.Record => await dbContext.Forms.AnyAsync(item => item.Id == resourceId, cancellationToken),
                AccessPolicyResourceTypes.Report => await dbContext.Reports.AnyAsync(item => item.Id == resourceId, cancellationToken),
                _ => false
            };
            if (!exists) throw new AccessPolicyException(400, "The policy resource was not found.");
        }
        return new(name, description, resourceType, request.ResourceId, action, conditions, request.Priority, request.IsEnabled);
    }

    private static void ValidateResource(string resourceType, Guid? resourceId, string action)
    {
        if (!AccessPolicyResourceTypes.Supported.Contains(resourceType)) throw new AccessPolicyException(400, "Resource type is invalid.");
        var valid = resourceType switch
        {
            AccessPolicyResourceTypes.Platform => resourceId is null && PlatformPermissions.AllBuiltInPermissions.Contains(action),
            AccessPolicyResourceTypes.Form => PlatformPermissions.FormActions.Contains(action),
            AccessPolicyResourceTypes.Record => AccessPolicyActions.Record.Contains(action),
            AccessPolicyResourceTypes.Report => PlatformPermissions.ReportActions.Contains(action),
            _ => false
        };
        if (!valid) throw new AccessPolicyException(400, "The resource ID or action is invalid for this policy type.");
    }

    private static AccessPolicyConditions NormalizeConditions(AccessPolicyConditions value) => new(
        NormalizeStrings(value.RoleAny), NormalizeStrings(value.MembershipRoleAny), NormalizeIds(value.DepartmentAny),
        NormalizeIds(value.GroupAny), NormalizeStrings(value.RecordStatusAny), value.RecordOwnerIsCurrentUser);

    private static void ValidateConditionBounds(AccessPolicyConditions conditions)
    {
        var collections = new[]
        {
            conditions.RoleAny?.Count ?? 0,
            conditions.MembershipRoleAny?.Count ?? 0,
            conditions.DepartmentAny?.Count ?? 0,
            conditions.GroupAny?.Count ?? 0,
            conditions.RecordStatusAny?.Count ?? 0
        };
        if (collections.Any(count => count > 100)
            || conditions.RoleAny?.Any(value => value.Length > 200) == true
            || conditions.RecordStatusAny?.Any(value => value.Length > 120) == true)
            throw new AccessPolicyException(400, "Policy condition lists are too large or contain oversized values.");
    }

    private static IReadOnlyCollection<string> NormalizeStrings(IReadOnlyCollection<string>? values) =>
        (values ?? Array.Empty<string>()).Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal).OrderBy(value => value).ToArray();
    private static IReadOnlyCollection<Guid> NormalizeIds(IReadOnlyCollection<Guid>? values) =>
        (values ?? Array.Empty<Guid>()).Where(value => value != Guid.Empty).Distinct().OrderBy(value => value).ToArray();
    private static string Required(string? value, string label, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max ? value.Trim() : throw new AccessPolicyException(400, $"{label} is required and must be at most {max} characters.");

    private static AccessPolicyDto ToDto(AccessPolicy policy) => new(
        policy.Id, policy.Name, policy.Description, policy.ResourceType, policy.ResourceId, policy.Action,
        AccessPolicyEvaluator.Deserialize(policy), policy.Priority, policy.IsEnabled, policy.ConcurrencyStamp, policy.CreatedAt, policy.UpdatedAt);

    private void AddAudit(Guid id, string action, Guid? actor, AccessPolicy policy) => dbContext.AuditLogs.Add(new AuditLogEntry
    {
        Id = Guid.NewGuid(), EntityType = "AccessPolicy", EntityId = id, Action = action, UserId = actor,
        MetadataJson = JsonSerializer.SerializeToDocument(new { policy.ResourceType, policy.ResourceId, policy.Action, policy.IsEnabled, policy.Priority })
    });

    private sealed record ValidatedPolicy(string Name, string? Description, string ResourceType, Guid? ResourceId, string Action, AccessPolicyConditions Conditions, int Priority, bool IsEnabled);
}
