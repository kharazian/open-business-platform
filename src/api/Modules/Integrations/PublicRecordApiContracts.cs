namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class PublicRecordApiVersions
{
    public const string V1 = "v1";
}

public sealed record PublicCreateRecordRequest(IReadOnlyDictionary<string, object?> Values);

public sealed record PublicRecordResponse(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string Status,
    IReadOnlyDictionary<string, object?> Values,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
