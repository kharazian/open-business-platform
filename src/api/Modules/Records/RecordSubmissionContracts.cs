using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record SubmitRecordRequest(IReadOnlyDictionary<string, object?> Values);

public sealed record FormRecordDto(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string Status,
    IReadOnlyDictionary<string, object?> Values,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById);

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
