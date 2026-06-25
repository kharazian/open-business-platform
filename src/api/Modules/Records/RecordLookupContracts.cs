namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record RecordLookupOptionsRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    IReadOnlyDictionary<string, string?>? DependencyValues = null);

public sealed record RecordLookupOptionDto(
    Guid RecordId,
    string Label,
    string? Description);
