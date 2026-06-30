using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record ListRecordsRequest(int Page = 1, int PageSize = 25, string? Search = null);

public sealed record ListSubTableRowsRequest(
    int Page = 1,
    int PageSize = 25,
    string? SortFieldId = null,
    string? SortDirection = null,
    IReadOnlyDictionary<string, string?>? Filters = null);

public sealed record SubmitRecordRequest(IReadOnlyDictionary<string, object?> Values);

public sealed record UpdateRecordRequest(IReadOnlyDictionary<string, object?> Values, string ConcurrencyStamp);

public sealed record AssignRecordRequest(Guid? AssignedToUserId, Guid? AssignedGroupId, string ConcurrencyStamp);

public sealed record ChangeRecordStatusRequest(string Status, string ConcurrencyStamp);

public sealed record FormRecordListItemDto(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string Status,
    Guid? OwnerId,
    Guid? DepartmentId,
    Guid? AssignedToUserId,
    Guid? AssignedGroupId,
    IReadOnlyDictionary<string, object?> Values,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    IReadOnlyDictionary<string, string>? DisplayValues = null);

public sealed record FormRecordDto(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string Status,
    Guid? OwnerId,
    Guid? DepartmentId,
    Guid? AssignedToUserId,
    Guid? AssignedGroupId,
    IReadOnlyDictionary<string, object?> Values,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    IReadOnlyDictionary<string, string>? DisplayValues = null);

public sealed record FormRecordDetailDto(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string Status,
    Guid? OwnerId,
    Guid? DepartmentId,
    Guid? AssignedToUserId,
    Guid? AssignedGroupId,
    IReadOnlyDictionary<string, object?> Values,
    FormSchemaDefinition Schema,
    IReadOnlyCollection<string> ReadOnlyFieldIds,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById,
    IReadOnlyDictionary<string, string>? DisplayValues = null);

public sealed record SubTableColumnDto(
    string FieldId,
    string Label,
    string Type);

public sealed record SubTableRowDto(
    Guid RecordId,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyDictionary<string, string>? DisplayValues,
    DateTimeOffset CreatedAt);

public sealed record SubTableRowsDto(
    string FieldId,
    IReadOnlyList<SubTableColumnDto> Columns,
    long TotalCount,
    IReadOnlyList<SubTableRowDto> Items);

public sealed record RecordErrorResponse(string Message, IReadOnlyList<FormValidationError>? Errors = null);

public sealed class RecordSubmissionException : Exception
{
    public RecordSubmissionException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<FormValidationError>();
    }

    public RecordSubmissionException(int statusCode, string message, IReadOnlyList<FormValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<FormValidationError> Errors { get; }
}

public sealed class RecordQueryException : Exception
{
    public RecordQueryException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<FormValidationError>();
    }

    public RecordQueryException(int statusCode, string message, IReadOnlyList<FormValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<FormValidationError> Errors { get; }
}

public sealed class RecordMutationException : Exception
{
    public RecordMutationException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<FormValidationError>();
    }

    public RecordMutationException(int statusCode, string message, IReadOnlyList<FormValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<FormValidationError> Errors { get; }
}
