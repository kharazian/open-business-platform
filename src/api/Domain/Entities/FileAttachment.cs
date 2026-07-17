using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class FileAttachment : WorkspaceCreationAuditedEntity<Guid>
{
    public Guid FormId { get; set; }
    public FormDefinition? Form { get; set; }
    public Guid FormVersionId { get; set; }
    public FormVersion? FormVersion { get; set; }
    public Guid? RecordId { get; set; }
    public FormRecord? Record { get; set; }
    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public string FieldId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = AttachmentStorageProviders.Postgres;
    public string StorageKey { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ScanStatus { get; set; } = AttachmentScanStatuses.Accepted;
    public string Status { get; set; } = AttachmentStatuses.Pending;
    public DateTimeOffset? AttachedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}

public static class AttachmentStorageProviders { public const string Postgres = "postgres"; }
public static class AttachmentScanStatuses { public const string Accepted = "accepted"; public const string Rejected = "rejected"; }
public static class AttachmentStatuses { public const string Pending = "pending"; public const string Attached = "attached"; public const string Removed = "removed"; public const string Deleted = "deleted"; }
