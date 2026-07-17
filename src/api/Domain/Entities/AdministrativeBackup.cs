using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class AdministrativeBackup : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public JsonDocument ManifestJson { get; set; } = null!;
    public string ArtifactContent { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public sealed class RestorePlan : WorkspaceCreationAuditedEntity<Guid>, IHasExtraProperties
{
    public Guid BackupId { get; set; }
    public AdministrativeBackup? Backup { get; set; }
    public string Status { get; set; } = string.Empty;
    public JsonDocument ValidationJson { get; set; } = null!;
    public DateTimeOffset PlannedAt { get; set; }
    public JsonDocument? ExtraPropertiesJson { get; set; }
}
