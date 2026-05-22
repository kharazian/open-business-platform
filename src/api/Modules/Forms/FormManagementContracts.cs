namespace OpenBusinessPlatform.Api.Modules.Forms;

public sealed record FormSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    int FieldCount,
    Guid? CurrentVersionId,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record FormDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    int FieldCount,
    Guid? CurrentVersionId,
    FormSchemaDefinition? DraftSchema,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record CreateFormRequest(string Name, string? Description);

public sealed record UpdateFormDraftRequest(FormSchemaDefinition Schema);

public sealed record PublishedFormVersionDto(
    Guid Id,
    Guid FormId,
    int VersionNumber,
    FormSchemaDefinition Schema,
    Guid? PublishedById,
    DateTimeOffset PublishedAt);

public sealed record PublishFormResponse(FormDetailDto Form, PublishedFormVersionDto Version);

public sealed record PublishedFormSubmissionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CurrentVersionId,
    int CurrentVersionNumber,
    FormSchemaDefinition Schema);

public sealed record FormErrorResponse(string Message, IReadOnlyList<FormValidationError>? Errors = null);

public sealed class FormManagementException : Exception
{
    public FormManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<FormValidationError>();
    }

    public FormManagementException(int statusCode, string message, IReadOnlyList<FormValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<FormValidationError> Errors { get; }
}
