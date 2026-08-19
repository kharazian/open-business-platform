namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record RelatedRecordColumnDto(
    string FieldId,
    string Label,
    string Type);

public sealed record RelatedRecordPanelDto(
    Guid SourceFormId,
    string SourceFormName,
    string SourceFieldId,
    string SourceFieldLabel,
    IReadOnlyList<RelatedRecordColumnDto> Columns,
    long TotalCount);

public sealed record RelatedRecordRowDto(
    Guid RecordId,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Cells);

public sealed record RelatedRecordRowsDto(
    RelatedRecordPanelDto Panel,
    int Page,
    int PageSize,
    IReadOnlyList<RelatedRecordRowDto> Items);
