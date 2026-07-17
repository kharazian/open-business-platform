using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class FileAttachmentService(
    OpenBusinessPlatformDbContext dbContext,
    IFileAttachmentContentStore contentStore,
    IFileAttachmentScanner scanner)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<FileAttachmentDto> UploadAsync(Guid formId, string fieldId, Guid? targetRecordId, IFormFile file, Guid? actorId, CancellationToken ct)
    {
        if (actorId is null) throw new FileAttachmentException(StatusCodes.Status403Forbidden, "A local user identity is required to upload files.");
        FormVersion version;
        if (targetRecordId is not null)
        {
            var target = await dbContext.Records.AsNoTracking().Include(item => item.FormVersion).FirstOrDefaultAsync(item => item.Id == targetRecordId && item.FormId == formId && !item.IsDeleted, ct);
            version = target?.FormVersion ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "Target record was not found.");
        }
        else
        {
            var form = await dbContext.Forms.Include(item => item.CurrentVersion).FirstOrDefaultAsync(item => item.Id == formId && !item.IsDeleted, ct);
            if (form?.CurrentVersion is null || form.Status != FormStatuses.Published)
                throw new FileAttachmentException(StatusCodes.Status409Conflict, "A published form is required to upload files.");
            version = form.CurrentVersion;
        }
        var schema = DeserializeSchema(version.SchemaJson) ?? throw new FileAttachmentException(StatusCodes.Status409Conflict, "Published form schema is invalid.");
        var field = schema.Fields.FirstOrDefault(item => item.Id == fieldId && item.Type == FormFieldTypes.FileUpload)
            ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "File field was not found.");
        var config = field.FileUpload ?? new FormFieldFileUploadDefinition();
        if (file.Length < 1 || file.Length > config.MaxSizeBytes || file.Length > FormFileUploadLimits.MaxSizeBytes)
            throw Validation(fieldId, "attachment.too_large", $"The selected file must be between 1 and {config.MaxSizeBytes} bytes.");

        await using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var content = buffer.ToArray();
        var inspection = scanner.Inspect(file.FileName, file.ContentType, content, config);
        if (!inspection.Accepted) throw Validation(fieldId, inspection.ErrorCode ?? "attachment.rejected", inspection.ErrorMessage ?? "The selected file was rejected.");

        var attachment = new FileAttachment
        {
            Id = Guid.NewGuid(),
            FormId = formId,
            FormVersionId = version.Id,
            UploadedById = actorId.Value,
            FieldId = field.Id,
            FileName = NormalizeFileName(file.FileName),
            ContentType = inspection.ContentType!,
            SizeBytes = content.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(),
            ScanStatus = AttachmentScanStatuses.Accepted,
            Status = AttachmentStatuses.Pending,
            CreatedById = actorId
        };
        contentStore.Store(attachment, content);
        dbContext.FileAttachments.Add(attachment);
        Audit(attachment.Id, "file_attachment_uploaded", actorId, new { attachment.FormId, attachment.FormVersionId, attachment.FieldId, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.Sha256 });
        await dbContext.SaveChangesAsync(ct);
        return ToDto(attachment);
    }

    public async Task<FileAttachmentDto> GetMetadataAsync(Guid id, ClaimsPrincipal principal, Guid? actorId, PermissionService permissions, CancellationToken ct)
    {
        var attachment = await FindMetadataAsync(id, ct) ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        await EnsureCanReadAsync(attachment, principal, actorId, permissions, ct);
        return ToDto(attachment);
    }

    public async Task<FileAttachmentDownload> DownloadAsync(Guid id, ClaimsPrincipal principal, Guid? actorId, PermissionService permissions, CancellationToken ct)
    {
        var attachment = await FindMetadataAsync(id, ct) ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        await EnsureCanReadAsync(attachment, principal, actorId, permissions, ct);
        var content = await contentStore.ReadAsync(id, attachment.Status, ct) ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment content was not found.");
        Audit(id, "file_attachment_downloaded", actorId, new { attachment.RecordId, attachment.FieldId, attachment.FileName, attachment.SizeBytes, attachment.Sha256 });
        await dbContext.SaveChangesAsync(ct);
        return new(content, attachment.ContentType, attachment.FileName);
    }

    public async Task DeletePendingAsync(Guid id, Guid? actorId, CancellationToken ct)
    {
        var attachment = await FindMetadataAsync(id, ct)
            ?? throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        if (actorId is null || attachment.UploadedById != actorId || attachment.Status != AttachmentStatuses.Pending)
            throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        var affected = await dbContext.FileAttachments.Where(item => item.Id == id && item.UploadedById == actorId && item.Status == AttachmentStatuses.Pending)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, AttachmentStatuses.Deleted).SetProperty(item => item.RemovedAt, DateTimeOffset.UtcNow).SetProperty(item => item.Content, Array.Empty<byte>()), ct);
        if (affected != 1) throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        Audit(id, "file_attachment_pending_deleted", actorId, new { attachment.FormId, attachment.FieldId, attachment.FileName });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FormValidationError>> ValidateAndClaimAsync(
        Guid recordId,
        Guid formId,
        Guid formVersionId,
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, object?>? currentValues,
        IReadOnlyDictionary<string, object?> values,
        Guid? actorId,
        CancellationToken ct)
    {
        var errors = new List<FormValidationError>();
        var claims = new List<(FormFieldDefinition Field, FileAttachment Attachment)>();
        var removals = new List<(FormFieldDefinition Field, FileAttachment Attachment)>();
        foreach (var field in schema.Fields.Where(item => item.Type == FormFieldTypes.FileUpload))
        {
            var current = currentValues is not null && currentValues.TryGetValue(field.Id, out var currentValue) ? AsString(currentValue) : null;
            var requested = values.TryGetValue(field.Id, out var value) ? AsString(value) : null;
            if (string.Equals(current, requested, StringComparison.Ordinal)) continue;

            if (!string.IsNullOrWhiteSpace(requested))
            {
                if (!Guid.TryParse(requested, out var attachmentId)) { errors.Add(Error(field, "record.attachment_reference", "The attachment reference is invalid.")); continue; }
                var attachment = await FindMetadataAsync(attachmentId, ct);
                if (attachment is null || actorId is null || attachment.UploadedById != actorId || attachment.FormId != formId || attachment.FormVersionId != formVersionId || attachment.FieldId != field.Id || attachment.Status != AttachmentStatuses.Pending || attachment.ScanStatus != AttachmentScanStatuses.Accepted)
                { errors.Add(Error(field, "record.attachment_unavailable", "The attachment was not found or cannot be claimed.")); continue; }
                claims.Add((field, attachment));
            }

            if (Guid.TryParse(current, out var currentId))
            {
                var attached = await FindMetadataAsync(currentId, ct);
                if (attached is not null && (attached.RecordId != recordId || attached.FieldId != field.Id || attached.Status != AttachmentStatuses.Attached)) attached = null;
                if (attached is not null) removals.Add((field, attached));
            }
        }
        if (errors.Count > 0) return errors;

        var now = DateTimeOffset.UtcNow;
        foreach (var (field, attachment) in removals)
        {
            await dbContext.FileAttachments.Where(item => item.Id == attachment.Id && item.RecordId == recordId && item.Status == AttachmentStatuses.Attached)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, AttachmentStatuses.Removed).SetProperty(item => item.RemovedAt, now), ct);
            Audit(attachment.Id, "file_attachment_removed", actorId, new { RecordId = recordId, FieldId = field.Id, attachment.FileName });
        }
        foreach (var (field, attachment) in claims)
        {
            var affected = await dbContext.FileAttachments.Where(item => item.Id == attachment.Id && item.Status == AttachmentStatuses.Pending)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.RecordId, recordId).SetProperty(item => item.Status, AttachmentStatuses.Attached).SetProperty(item => item.AttachedAt, now), ct);
            if (affected != 1) return [Error(field, "record.attachment_unavailable", "The attachment was claimed by another request.")];
            Audit(attachment.Id, "file_attachment_claimed", actorId, new { RecordId = recordId, FieldId = field.Id, attachment.FileName });
        }
        return errors;
    }

    public static string NormalizeFileName(string value)
    {
        var name = Path.GetFileName(value.Replace('\\', '/')).Trim();
        name = new string(name.Where(character => !char.IsControl(character) && character is not '/' and not '\\').ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..") name = "attachment";
        if (name.Length <= 180) return name;
        var extension = Path.GetExtension(name);
        return extension.Length is > 0 and < 30 ? $"{Path.GetFileNameWithoutExtension(name)[..(180 - extension.Length)]}{extension}" : name[..180];
    }

    private async Task<FileAttachment?> FindMetadataAsync(Guid id, CancellationToken ct) => await dbContext.FileAttachments.AsNoTracking().Where(item => item.Id == id).Select(item => new FileAttachment
    {
        Id = item.Id, WorkspaceId = item.WorkspaceId, FormId = item.FormId, FormVersionId = item.FormVersionId, RecordId = item.RecordId, FieldId = item.FieldId,
        FileName = item.FileName, ContentType = item.ContentType, SizeBytes = item.SizeBytes, Sha256 = item.Sha256, StorageProvider = item.StorageProvider,
        StorageKey = item.StorageKey, ScanStatus = item.ScanStatus, Status = item.Status, CreatedAt = item.CreatedAt, CreatedById = item.CreatedById, UploadedById = item.UploadedById,
        AttachedAt = item.AttachedAt, RemovedAt = item.RemovedAt
    }).FirstOrDefaultAsync(ct);

    private async Task EnsureCanReadAsync(FileAttachment attachment, ClaimsPrincipal principal, Guid? actorId, PermissionService permissions, CancellationToken ct)
    {
        if (attachment.Status == AttachmentStatuses.Pending)
        {
            if (actorId != attachment.UploadedById) throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
            return;
        }
        if (attachment.Status != AttachmentStatuses.Attached || attachment.RecordId is null) throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        var record = await dbContext.Records.AsNoTracking().FirstOrDefaultAsync(item => item.Id == attachment.RecordId && !item.IsDeleted, ct);
        if (record is null || !await permissions.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.View, ct))
            throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
        var fieldAccess = await permissions.GetFieldAccessAsync(principal, attachment.FormId, ct);
        if (fieldAccess.HiddenFieldIds.Contains(attachment.FieldId)) throw new FileAttachmentException(StatusCodes.Status404NotFound, "Attachment was not found.");
    }

    private void Audit(Guid id, string action, Guid? actorId, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "FileAttachment", EntityId = id, Action = action, UserId = actorId, MetadataJson = JsonSerializer.SerializeToDocument(metadata, JsonOptions) });
    private static FileAttachmentDto ToDto(FileAttachment item) => new(item.Id, item.FormId, item.FormVersionId, item.RecordId, item.FieldId, item.FileName, item.ContentType, item.SizeBytes, item.Sha256, item.ScanStatus, item.Status, item.CreatedAt);
    private static FileAttachmentException Validation(string fieldId, string code, string message) => new(StatusCodes.Status400BadRequest, "Attachment is invalid.", [new($"values.{fieldId}", code, message)]);
    private static FormValidationError Error(FormFieldDefinition field, string code, string message) => new($"values.{field.Id}", code, $"'{field.Label}': {message}");
    private static string? AsString(object? value) => value switch { null => null, string text => text, JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(), _ => null };
    private static FormSchemaDefinition? DeserializeSchema(JsonDocument schema) { try { return JsonSerializer.Deserialize<FormSchemaDefinition>(schema.RootElement.GetRawText(), JsonOptions); } catch (JsonException) { return null; } }
}
