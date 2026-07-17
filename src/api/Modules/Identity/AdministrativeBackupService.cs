using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class AdministrativeBackupScopes
{
    public const string ConfigurationOnly = "configuration_only";
    public const string Full = "full";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { ConfigurationOnly, Full };
}

public sealed record CreateAdministrativeBackupRequest(string Scope);
public sealed record AdministrativeBackupDto(Guid Id, string Scope, string Status, string FileName, long SizeBytes, string Sha256, JsonElement Manifest, DateTimeOffset CompletedAt, DateTimeOffset CreatedAt);
public sealed record BackupDownloadDto(string FileName, string ContentType, string Content);
public sealed record RestorePlanDto(Guid Id, Guid BackupId, string Status, JsonElement Validation, DateTimeOffset PlannedAt);
public sealed record BackupManifest(int FormatVersion, Guid WorkspaceId, DateTimeOffset CreatedAt, string Scope, IReadOnlyCollection<string> Modules, IReadOnlyDictionary<string, int> EntityCounts, string PayloadSha256);
public sealed class AdministrativeBackupException(int statusCode, string message) : Exception(message) { public int StatusCode { get; } = statusCode; }

public sealed class AdministrativeBackupService(OpenBusinessPlatformDbContext dbContext, PermissionService permissions)
{
    private const int MaxArtifactBytes = 25 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions SnapshotJsonOptions = CreateSnapshotJsonOptions();
    private static readonly IReadOnlySet<string> SupportedModules = new HashSet<string>(StringComparer.Ordinal)
    {
        "forms", "formVersions", "reports", "dashboards", "triggers", "workflows",
        "workflowVersions", "printTemplates", "printTemplateVersions", "records"
    };

    public async Task<IReadOnlyCollection<AdministrativeBackupDto>> ListAsync(CancellationToken ct) =>
        (await dbContext.AdministrativeBackups.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToArrayAsync(ct)).Select(ToDto).ToArray();

    public async Task<AdministrativeBackupDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var backup = await dbContext.AdministrativeBackups.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return backup is null ? null : ToDto(backup);
    }

    public async Task<AdministrativeBackupDto> CreateAsync(ClaimsPrincipal principal, CreateAdministrativeBackupRequest request, Guid? actor, CancellationToken ct)
    {
        var scope = request.Scope?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AdministrativeBackupScopes.Supported.Contains(scope)) throw new AdministrativeBackupException(400, "Backup scope is invalid.");
        if (scope == AdministrativeBackupScopes.Full && !await permissions.CanAsync(principal, PlatformPermissions.Forms.ManageAll, ct))
            throw new AdministrativeBackupException(403, "Full backups require workspace-wide form management.");

        var workspace = await dbContext.Workspaces.AsNoTracking().SingleAsync(x => x.Id == dbContext.ActiveWorkspaceId, ct);
        var forms = await dbContext.Forms.AsNoTracking().ToArrayAsync(ct);
        var formVersions = await dbContext.FormVersions.AsNoTracking().ToArrayAsync(ct);
        var reports = await dbContext.Reports.AsNoTracking().ToArrayAsync(ct);
        var dashboards = await dbContext.Dashboards.AsNoTracking().ToArrayAsync(ct);
        var triggers = await dbContext.Triggers.AsNoTracking().ToArrayAsync(ct);
        var workflows = await dbContext.Workflows.AsNoTracking().ToArrayAsync(ct);
        var workflowVersions = await dbContext.WorkflowVersions.AsNoTracking().ToArrayAsync(ct);
        var printTemplates = await dbContext.PrintTemplates.AsNoTracking().ToArrayAsync(ct);
        var printVersions = await dbContext.PrintTemplateVersions.AsNoTracking().ToArrayAsync(ct);
        var records = scope == AdministrativeBackupScopes.Full
            ? await LoadFullyAuthorizedRecordsAsync(principal, forms, ct)
            : Array.Empty<FormRecord>();
        var modules = new Dictionary<string, object>
        {
            ["forms"] = forms, ["formVersions"] = formVersions, ["reports"] = reports,
            ["dashboards"] = dashboards, ["triggers"] = triggers, ["workflows"] = workflows,
            ["workflowVersions"] = workflowVersions, ["printTemplates"] = printTemplates,
            ["printTemplateVersions"] = printVersions
        };
        if (scope == AdministrativeBackupScopes.Full) modules["records"] = records;
        var payloadJson = JsonSerializer.Serialize(modules, SnapshotJsonOptions);
        var payloadSha = Sha256(payloadJson);
        var counts = modules.ToDictionary(pair => pair.Key, pair => ((System.Collections.ICollection)pair.Value).Count, StringComparer.Ordinal);
        var manifest = new BackupManifest(1, workspace.Id, DateTimeOffset.UtcNow, scope, modules.Keys.ToArray(), counts, payloadSha);
        var artifact = JsonSerializer.Serialize(new { manifest, workspace = new { workspace.Id, workspace.TenantId, workspace.Name, workspace.Slug }, data = modules }, SnapshotJsonOptions);
        var size = Encoding.UTF8.GetByteCount(artifact);
        if (size > MaxArtifactBytes) throw new AdministrativeBackupException(413, "Backup artifact exceeds the 25 MiB task limit.");
        var backup = new AdministrativeBackup
        {
            Id = Guid.NewGuid(), Scope = scope, Status = "succeeded",
            FileName = $"workspace-{workspace.Slug}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json",
            SizeBytes = size, Sha256 = Sha256(artifact), ManifestJson = JsonSerializer.SerializeToDocument(manifest, JsonOptions),
            ArtifactContent = artifact, CompletedAt = DateTimeOffset.UtcNow, CreatedById = actor
        };
        dbContext.AdministrativeBackups.Add(backup);
        Audit(backup.Id, "administrative_backup_created", actor, new { scope, sizeBytes = size, backup.Sha256, counts });
        await dbContext.SaveChangesAsync(ct); return ToDto(backup);
    }

    public async Task<BackupDownloadDto?> DownloadAsync(Guid id, Guid? actor, CancellationToken ct)
    {
        var backup = await dbContext.AdministrativeBackups.SingleOrDefaultAsync(x => x.Id == id, ct); if (backup is null) return null;
        if (Sha256(backup.ArtifactContent) != backup.Sha256) throw new AdministrativeBackupException(409, "Backup artifact checksum validation failed.");
        Audit(id, "administrative_backup_downloaded", actor, new { backup.FileName, backup.SizeBytes, backup.Sha256 }); await dbContext.SaveChangesAsync(ct);
        return new(backup.FileName, backup.ContentType, backup.ArtifactContent);
    }

    public async Task<RestorePlanDto?> PlanRestoreAsync(Guid backupId, Guid? actor, CancellationToken ct)
    {
        var backup = await dbContext.AdministrativeBackups.AsNoTracking().SingleOrDefaultAsync(x => x.Id == backupId, ct); if (backup is null) return null;
        var errors = new List<string>(); var warnings = new List<string>(); var conflicts = new Dictionary<string, int>();
        if (Sha256(backup.ArtifactContent) != backup.Sha256) errors.Add("artifact_checksum_mismatch");
        try
        {
            using var artifact = JsonDocument.Parse(backup.ArtifactContent);
            var manifest = artifact.RootElement.GetProperty("manifest").Deserialize<BackupManifest>(JsonOptions) ?? throw new JsonException();
            if (manifest.FormatVersion != 1) errors.Add("unsupported_format_version");
            if (manifest.WorkspaceId != dbContext.ActiveWorkspaceId) errors.Add("workspace_mismatch");
            var data = artifact.RootElement.GetProperty("data");
            var payload = JsonSerializer.Serialize(data, JsonOptions);
            if (Sha256(payload) != manifest.PayloadSha256) errors.Add("payload_checksum_mismatch");
            foreach (var module in manifest.Modules)
            {
                if (!SupportedModules.Contains(module)) { errors.Add($"unsupported_module:{module}"); continue; }
                if (!data.TryGetProperty(module, out var items) || items.ValueKind != JsonValueKind.Array) { errors.Add($"missing_module:{module}"); continue; }
                if (!manifest.EntityCounts.TryGetValue(module, out var expectedCount) || expectedCount != items.GetArrayLength()) errors.Add($"count_mismatch:{module}");
                var ids = items.EnumerateArray().Where(item => item.TryGetProperty("id", out _)).Select(item => item.GetProperty("id").GetGuid()).ToArray();
                conflicts[module] = module switch
                {
                    "forms" => await dbContext.Forms.CountAsync(x => ids.Contains(x.Id), ct),
                    "formVersions" => await dbContext.FormVersions.CountAsync(x => ids.Contains(x.Id), ct),
                    "reports" => await dbContext.Reports.CountAsync(x => ids.Contains(x.Id), ct),
                    "dashboards" => await dbContext.Dashboards.CountAsync(x => ids.Contains(x.Id), ct),
                    "triggers" => await dbContext.Triggers.CountAsync(x => ids.Contains(x.Id), ct),
                    "workflows" => await dbContext.Workflows.CountAsync(x => ids.Contains(x.Id), ct),
                    "workflowVersions" => await dbContext.WorkflowVersions.CountAsync(x => ids.Contains(x.Id), ct),
                    "printTemplates" => await dbContext.PrintTemplates.CountAsync(x => ids.Contains(x.Id), ct),
                    "printTemplateVersions" => await dbContext.PrintTemplateVersions.CountAsync(x => ids.Contains(x.Id), ct),
                    "records" => await dbContext.Records.CountAsync(x => ids.Contains(x.Id), ct),
                    _ => 0
                };
            }
            warnings.Add("validation_only_no_changes_applied");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            errors.Add("artifact_json_invalid");
        }
        var status = errors.Count == 0 ? "valid" : "invalid";
        var validation = JsonSerializer.SerializeToDocument(new { errors, warnings, conflicts, canApply = false }, JsonOptions);
        var plan = new RestorePlan { Id = Guid.NewGuid(), BackupId = backupId, Status = status, ValidationJson = validation, PlannedAt = DateTimeOffset.UtcNow, CreatedById = actor };
        dbContext.RestorePlans.Add(plan); Audit(plan.Id, "restore_plan_created", actor, new { backupId, status, errors, conflicts }); await dbContext.SaveChangesAsync(ct);
        return new(plan.Id, plan.BackupId, plan.Status, plan.ValidationJson.RootElement.Clone(), plan.PlannedAt);
    }

    private static string Sha256(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    private async Task<FormRecord[]> LoadFullyAuthorizedRecordsAsync(
        ClaimsPrincipal principal,
        IReadOnlyCollection<FormDefinition> forms,
        CancellationToken ct)
    {
        var result = new List<FormRecord>();
        foreach (var form in forms)
        {
            if (!await permissions.CanAccessFormAsync(principal, form.Id, PlatformPermissions.Form.Export, ct))
                throw new AdministrativeBackupException(403, "A form policy prevents a complete full backup.");
            var source = dbContext.Records.AsNoTracking().Where(record => record.FormId == form.Id);
            var sourceCount = await source.CountAsync(ct);
            var allowed = await permissions.ApplyRecordAccessAsync(principal, source, form.Id, PlatformPermissions.Form.Export, ct);
            var rows = await allowed.ToArrayAsync(ct);
            if (rows.Length != sourceCount)
                throw new AdministrativeBackupException(403, "A record policy prevents a complete full backup.");
            result.AddRange(rows);
        }
        return result.ToArray();
    }
    private static JsonSerializerOptions CreateSnapshotJsonOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
            {
                if (typeInfo.Properties[index].Name is "extraPropertiesJson" or "passwordHash" or "keyHash" or "artifactContent")
                    typeInfo.Properties.RemoveAt(index);
            }
        });
        return new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver };
    }
    private static AdministrativeBackupDto ToDto(AdministrativeBackup x) => new(x.Id, x.Scope, x.Status, x.FileName, x.SizeBytes, x.Sha256, x.ManifestJson.RootElement.Clone(), x.CompletedAt, x.CreatedAt);
    private void Audit(Guid id, string action, Guid? actor, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "AdministrativeBackup", EntityId = id, Action = action, UserId = actor, MetadataJson = JsonSerializer.SerializeToDocument(metadata, JsonOptions) });
}
