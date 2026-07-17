using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class RetentionResourceTypes
{
    public const string Record = "record";
    public const string AuditLog = "audit_log";
    public const string IntegrationLog = "integration_log";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Record, AuditLog, IntegrationLog };
}

public sealed record RetentionPolicyDto(Guid Id, string Name, string ResourceType, Guid? FormId, int RetentionDays, int Priority, bool IsEnabled, string ConcurrencyStamp);
public sealed record SaveRetentionPolicyRequest(string Name, string ResourceType, Guid? FormId, int RetentionDays, int Priority, bool IsEnabled, string? ConcurrencyStamp);
public sealed record LegalHoldDto(Guid Id, string ResourceType, Guid EntityId, string Reason, DateTimeOffset PlacedAt, Guid? PlacedById, DateTimeOffset? ReleasedAt, Guid? ReleasedById, string? ReleaseReason, string ConcurrencyStamp);
public sealed record PlaceLegalHoldRequest(string ResourceType, Guid EntityId, string Reason);
public sealed record ReleaseLegalHoldRequest(string ReleaseReason, string ConcurrencyStamp);
public sealed record RetentionDryRunDto(Guid PolicyId, string ResourceType, DateTimeOffset Cutoff, long CandidateCount, IReadOnlyCollection<Guid> SampleEntityIds, bool IsTruncated);

public sealed class RetentionException(int statusCode, string message) : Exception(message) { public int StatusCode { get; } = statusCode; }

public sealed class RetentionService(OpenBusinessPlatformDbContext dbContext)
{
    private const int SampleLimit = 100;

    public async Task<IReadOnlyCollection<RetentionPolicyDto>> ListPoliciesAsync(CancellationToken ct) =>
        (await dbContext.RetentionPolicies.AsNoTracking().OrderByDescending(x => x.Priority).ThenBy(x => x.Name).ToArrayAsync(ct)).Select(ToDto).ToArray();

    public async Task<RetentionPolicyDto> CreatePolicyAsync(SaveRetentionPolicyRequest request, Guid? actor, CancellationToken ct)
    {
        var values = await ValidatePolicyAsync(request, ct);
        var policy = new RetentionPolicy { Id = Guid.NewGuid(), Name = values.Name, ResourceType = values.Type, FormId = values.FormId, RetentionDays = values.Days, Priority = values.Priority, IsEnabled = values.Enabled, CreatedById = actor };
        dbContext.RetentionPolicies.Add(policy); Audit(policy.Id, "retention_policy_created", actor, new { policy.ResourceType, policy.FormId, policy.RetentionDays, policy.IsEnabled });
        await dbContext.SaveChangesAsync(ct); return ToDto(policy);
    }

    public async Task<RetentionPolicyDto?> UpdatePolicyAsync(Guid id, SaveRetentionPolicyRequest request, Guid? actor, CancellationToken ct)
    {
        var policy = await dbContext.RetentionPolicies.SingleOrDefaultAsync(x => x.Id == id, ct); if (policy is null) return null;
        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp) || policy.ConcurrencyStamp != request.ConcurrencyStamp.Trim()) throw new DbUpdateConcurrencyException("The retention policy changed. Refresh and try again.");
        var values = await ValidatePolicyAsync(request, ct); policy.Name = values.Name; policy.ResourceType = values.Type; policy.FormId = values.FormId; policy.RetentionDays = values.Days; policy.Priority = values.Priority; policy.IsEnabled = values.Enabled; policy.UpdatedById = actor;
        Audit(policy.Id, "retention_policy_updated", actor, new { policy.ResourceType, policy.FormId, policy.RetentionDays, policy.IsEnabled }); await dbContext.SaveChangesAsync(ct); return ToDto(policy);
    }

    public async Task<IReadOnlyCollection<LegalHoldDto>> ListHoldsAsync(CancellationToken ct) =>
        (await dbContext.LegalHolds.AsNoTracking().OrderByDescending(x => x.PlacedAt).ToArrayAsync(ct)).Select(ToDto).ToArray();

    public async Task<LegalHoldDto> PlaceHoldAsync(PlaceLegalHoldRequest request, Guid? actor, CancellationToken ct)
    {
        var type = NormalizeType(request.ResourceType); if (request.EntityId == Guid.Empty) throw new RetentionException(400, "Entity ID is required.");
        var exists = type switch { RetentionResourceTypes.Record => await dbContext.Records.AnyAsync(x => x.Id == request.EntityId, ct), RetentionResourceTypes.AuditLog => await dbContext.AuditLogs.AnyAsync(x => x.Id == request.EntityId, ct), _ => await dbContext.IntegrationLogs.AnyAsync(x => x.Id == request.EntityId, ct) };
        if (!exists) throw new RetentionException(400, "The legal-hold entity was not found.");
        if (await dbContext.LegalHolds.AnyAsync(x => x.ResourceType == type && x.EntityId == request.EntityId && x.ReleasedAt == null, ct)) throw new RetentionException(400, "An active legal hold already exists.");
        var hold = new LegalHold { Id = Guid.NewGuid(), ResourceType = type, EntityId = request.EntityId, Reason = Required(request.Reason, "Reason", 1000), PlacedAt = DateTimeOffset.UtcNow, PlacedById = actor, CreatedById = actor };
        dbContext.LegalHolds.Add(hold); Audit(hold.Id, "legal_hold_placed", actor, new { type, request.EntityId }); await dbContext.SaveChangesAsync(ct); return ToDto(hold);
    }

    public async Task<LegalHoldDto?> ReleaseHoldAsync(Guid id, ReleaseLegalHoldRequest request, Guid? actor, CancellationToken ct)
    {
        var hold = await dbContext.LegalHolds.SingleOrDefaultAsync(x => x.Id == id, ct); if (hold is null) return null;
        if (hold.ReleasedAt is not null) throw new RetentionException(400, "The legal hold is already released.");
        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp) || hold.ConcurrencyStamp != request.ConcurrencyStamp.Trim()) throw new DbUpdateConcurrencyException("The legal hold changed. Refresh and try again.");
        hold.ReleaseReason = Required(request.ReleaseReason, "Release reason", 1000); hold.ReleasedAt = DateTimeOffset.UtcNow; hold.ReleasedById = actor; hold.UpdatedById = actor;
        Audit(hold.Id, "legal_hold_released", actor, new { hold.ResourceType, hold.EntityId }); await dbContext.SaveChangesAsync(ct); return ToDto(hold);
    }

    public async Task<RetentionDryRunDto?> DryRunAsync(Guid policyId, Guid? actor, CancellationToken ct)
    {
        var policy = await dbContext.RetentionPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == policyId, ct); if (policy is null) return null;
        if (!policy.IsEnabled) throw new RetentionException(400, "Only enabled retention policies can be evaluated.");
        var cutoff = DateTimeOffset.UtcNow.AddDays(-policy.RetentionDays);
        var held = dbContext.LegalHolds.Where(x => x.ResourceType == policy.ResourceType && x.ReleasedAt == null).Select(x => x.EntityId);
        IQueryable<Guid> query = policy.ResourceType switch
        {
            RetentionResourceTypes.Record => dbContext.Records.Where(x => x.CreatedAt < cutoff && (policy.FormId == null || x.FormId == policy.FormId) && !held.Contains(x.Id)).Select(x => x.Id),
            RetentionResourceTypes.AuditLog => dbContext.AuditLogs.Where(x => x.CreatedAt < cutoff && !held.Contains(x.Id)).Select(x => x.Id),
            _ => dbContext.IntegrationLogs.Where(x => x.CreatedAt < cutoff && !held.Contains(x.Id)).Select(x => x.Id)
        };
        var count = await query.LongCountAsync(ct); var sample = await query.OrderBy(id => id).Take(SampleLimit).ToArrayAsync(ct);
        Audit(policy.Id, "retention_dry_run", actor, new { policy.ResourceType, policy.FormId, cutoff, candidateCount = count }); await dbContext.SaveChangesAsync(ct);
        return new(policy.Id, policy.ResourceType, cutoff, count, sample, count > sample.Length);
    }

    private async Task<(string Name, string Type, Guid? FormId, int Days, int Priority, bool Enabled)> ValidatePolicyAsync(SaveRetentionPolicyRequest request, CancellationToken ct)
    {
        var type = NormalizeType(request.ResourceType); if (request.RetentionDays is < 1 or > 36500) throw new RetentionException(400, "Retention days must be between 1 and 36500.");
        if (request.Priority is < -1000 or > 1000) throw new RetentionException(400, "Priority must be between -1000 and 1000.");
        if (type != RetentionResourceTypes.Record && request.FormId is not null) throw new RetentionException(400, "Only record retention policies may target a form.");
        if (request.FormId is { } formId && !await dbContext.Forms.AnyAsync(x => x.Id == formId, ct)) throw new RetentionException(400, "The retention form was not found.");
        return (Required(request.Name, "Name", 160), type, request.FormId, request.RetentionDays, request.Priority, request.IsEnabled);
    }
    private static string NormalizeType(string? value) { var type = value?.Trim().ToLowerInvariant() ?? ""; return RetentionResourceTypes.Supported.Contains(type) ? type : throw new RetentionException(400, "Retention resource type is invalid."); }
    private static string Required(string? value, string label, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max ? value.Trim() : throw new RetentionException(400, $"{label} is required and must be at most {max} characters.");
    private static RetentionPolicyDto ToDto(RetentionPolicy x) => new(x.Id, x.Name, x.ResourceType, x.FormId, x.RetentionDays, x.Priority, x.IsEnabled, x.ConcurrencyStamp);
    private static LegalHoldDto ToDto(LegalHold x) => new(x.Id, x.ResourceType, x.EntityId, x.Reason, x.PlacedAt, x.PlacedById, x.ReleasedAt, x.ReleasedById, x.ReleaseReason, x.ConcurrencyStamp);
    private void Audit(Guid id, string action, Guid? actor, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Retention", EntityId = id, Action = action, UserId = actor, MetadataJson = JsonSerializer.SerializeToDocument(metadata) });
}
