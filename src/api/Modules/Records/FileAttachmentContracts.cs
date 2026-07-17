namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record FileAttachmentDto(Guid Id, Guid FormId, Guid FormVersionId, Guid? RecordId, string FieldId, string FileName, string ContentType, long SizeBytes, string Sha256, string ScanStatus, string Status, DateTimeOffset CreatedAt);
public sealed record FileAttachmentDownload(byte[] Content, string ContentType, string FileName);

public sealed class FileAttachmentException(int statusCode, string message, IReadOnlyList<Forms.FormValidationError>? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public IReadOnlyList<Forms.FormValidationError> Errors { get; } = errors ?? Array.Empty<Forms.FormValidationError>();
}
