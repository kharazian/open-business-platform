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

public sealed record CreateFormRequest(string Name, string? Description);

public sealed record FormErrorResponse(string Message);

public sealed class FormManagementException : Exception
{
    public FormManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
