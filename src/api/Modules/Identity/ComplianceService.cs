using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Integrations;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed record ComplianceControlDto(string Key, string Title, string Status, string Summary);
public sealed record CompliancePostureDto(DateTimeOffset GeneratedAt, string Disclaimer, IReadOnlyCollection<ComplianceControlDto> Controls);
public sealed record ComplianceAuditQuery(DateTimeOffset? From, DateTimeOffset? To, string? EntityType, string? Action, Guid? UserId, int Page = 1, int PageSize = 50);
public sealed record ComplianceAuditEntryDto(Guid Id, string EntityType, Guid EntityId, string Action, Guid? UserId, IReadOnlyDictionary<string, object?>? Metadata, DateTimeOffset CreatedAt);
public sealed record ComplianceAuditPageDto(IReadOnlyCollection<ComplianceAuditEntryDto> Items, int Page, int PageSize, long Total);
public sealed record ComplianceAuditExportDto(string FileName, string ContentType, string Content);

public sealed class ComplianceException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ComplianceService(OpenBusinessPlatformDbContext dbContext)
{
    private const int MaxExportRows = 10_000;
    public async Task<CompliancePostureDto> GetPostureAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var activeAdmins = await dbContext.WorkspaceMemberships.AsNoTracking().CountAsync(item => item.WorkspaceId == dbContext.ActiveWorkspaceId && item.Status == WorkspaceMembershipStatuses.Active && (item.Role == WorkspaceMembershipRoles.Owner || item.Role == WorkspaceMembershipRoles.Admin), ct);
        var enabledSso = await dbContext.SsoProviders.AsNoTracking().CountAsync(item => item.IsEnabled, ct);
        var enabledPolicies = await dbContext.AccessPolicies.AsNoTracking().CountAsync(item => item.IsEnabled, ct);
        var enabledRetention = await dbContext.RetentionPolicies.AsNoTracking().CountAsync(item => item.IsEnabled, ct);
        var activeHolds = await dbContext.LegalHolds.AsNoTracking().CountAsync(item => item.ReleasedAt == null, ct);
        var latestBackup = await dbContext.AdministrativeBackups.AsNoTracking().OrderByDescending(item => item.CompletedAt).Select(item => (DateTimeOffset?)item.CompletedAt).FirstOrDefaultAsync(ct);
        var domainCounts = await dbContext.WorkspaceCustomDomains.AsNoTracking().GroupBy(_ => 1).Select(group => new { Total = group.Count(), Enabled = group.Count(item => item.Status == CustomDomainStatuses.Verified && item.IsEnabled), Pending = group.Count(item => item.Status != CustomDomainStatuses.Verified) }).SingleOrDefaultAsync(ct);
        var recentAuditCount = await dbContext.AuditLogs.AsNoTracking().LongCountAsync(item => item.CreatedAt >= now.AddDays(-30), ct);

        var controls = new[]
        {
            new ComplianceControlDto("administrators", "Administrative coverage", activeAdmins > 0 ? "pass" : "warning", activeAdmins > 0 ? $"{activeAdmins} active owner/admin membership(s)." : "No active owner or admin membership was found."),
            new ComplianceControlDto("sso", "Single sign-on", enabledSso > 0 ? "pass" : "info", enabledSso > 0 ? $"{enabledSso} enabled SSO provider(s)." : "SSO is optional and no provider is enabled."),
            new ComplianceControlDto("access-policies", "Enterprise access policies", enabledPolicies > 0 ? "pass" : "info", enabledPolicies > 0 ? $"{enabledPolicies} deny-overrides policy rule(s) enabled." : "No additional deny-overrides policy is enabled."),
            new ComplianceControlDto("retention", "Retention and legal holds", enabledRetention > 0 ? "pass" : "warning", $"{enabledRetention} enabled retention policy/policies; {activeHolds} active legal hold(s)."),
            new ComplianceControlDto("backup", "Administrative backup recency", latestBackup >= now.AddDays(-30) ? "pass" : "warning", latestBackup is null ? "No administrative backup exists." : $"Latest completed backup: {latestBackup.Value:O}."),
            new ComplianceControlDto("domains", "Custom-domain verification", domainCounts?.Pending > 0 ? "warning" : "pass", domainCounts is null ? "No custom domains configured." : $"{domainCounts.Enabled} enabled verified; {domainCounts.Pending} pending verification."),
            new ComplianceControlDto("audit", "Audit activity", recentAuditCount > 0 ? "pass" : "warning", $"{recentAuditCount} audit event(s) in the last 30 days.")
        };
        return new(now, "Operational posture only; this report is not a certification or legal assessment.", controls);
    }

    public async Task<ComplianceAuditPageDto> SearchAuditAsync(ComplianceAuditQuery request, CancellationToken ct)
    {
        var values = Validate(request); var query = Apply(dbContext.AuditLogs.AsNoTracking(), values);
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Skip((values.Page - 1) * values.PageSize).Take(values.PageSize).ToArrayAsync(ct);
        return new(rows.Select(ToDto).ToArray(), values.Page, values.PageSize, total);
    }

    public async Task<ComplianceAuditExportDto> ExportAuditAsync(ComplianceAuditQuery request, Guid? actorId, CancellationToken ct)
    {
        var values = Validate(request with { Page = 1, PageSize = 200 });
        var rows = await Apply(dbContext.AuditLogs.AsNoTracking(), values).OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Take(MaxExportRows + 1).ToArrayAsync(ct);
        if (rows.Length > MaxExportRows) throw new ComplianceException(413, $"Audit export exceeds the {MaxExportRows} row limit. Narrow the date range or filters.");
        var csv = new StringBuilder("id,created_at,entity_type,entity_id,action,user_id\n");
        foreach (var row in rows) csv.Append(Csv(row.Id.ToString())).Append(',').Append(Csv(row.CreatedAt.ToString("O"))).Append(',').Append(Csv(row.EntityType)).Append(',').Append(Csv(row.EntityId.ToString())).Append(',').Append(Csv(row.Action)).Append(',').Append(Csv(row.UserId?.ToString() ?? string.Empty)).Append('\n');
        dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Compliance", EntityId = Guid.NewGuid(), Action = "compliance_audit_exported", UserId = actorId, MetadataJson = JsonSerializer.SerializeToDocument(new { values.From, values.To, values.EntityType, values.Action, values.UserId, rowCount = rows.Length }) });
        await dbContext.SaveChangesAsync(ct);
        return new($"workspace-audit-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv", "text/csv; charset=utf-8", csv.ToString());
    }

    private static ComplianceAuditQuery Validate(ComplianceAuditQuery request)
    {
        var to = request.To ?? DateTimeOffset.UtcNow; var from = request.From ?? to.AddDays(-30);
        if (from > to) throw new ComplianceException(400, "Audit start time must be before end time.");
        if (to - from > TimeSpan.FromDays(366)) throw new ComplianceException(400, "Audit query range cannot exceed 366 days.");
        if (request.Page < 1 || request.PageSize is < 1 or > 200) throw new ComplianceException(400, "Audit page and page size are invalid.");
        return request with { From = from, To = to, EntityType = Optional(request.EntityType, 80), Action = Optional(request.Action, 80) };
    }
    private static IQueryable<AuditLogEntry> Apply(IQueryable<AuditLogEntry> query, ComplianceAuditQuery values)
    {
        query = query.Where(item => item.CreatedAt >= values.From && item.CreatedAt <= values.To);
        if (values.EntityType is not null) query = query.Where(item => item.EntityType == values.EntityType);
        if (values.Action is not null) query = query.Where(item => item.Action == values.Action);
        if (values.UserId is not null) query = query.Where(item => item.UserId == values.UserId);
        return query;
    }
    private static ComplianceAuditEntryDto ToDto(AuditLogEntry item) => new(item.Id, item.EntityType, item.EntityId, item.Action, item.UserId, Sanitize(item.MetadataJson), item.CreatedAt);
    private static IReadOnlyDictionary<string, object?>? Sanitize(JsonDocument? metadata) => metadata is null ? null : IntegrationMetadataSanitizer.Sanitize(JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata.RootElement.GetRawText()));
    private static string? Optional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw new ComplianceException(400, $"Audit filter must be at most {max} characters.");
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
