using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class RecordRelationship : WorkspaceAuditedEntity<Guid>
{
    public Guid SourceFormId { get; set; }
    public FormDefinition? SourceForm { get; set; }
    public Guid SourceFormVersionId { get; set; }
    public FormVersion? SourceFormVersion { get; set; }
    public Guid SourceRecordId { get; set; }
    public FormRecord? SourceRecord { get; set; }
    public string SourceFieldId { get; set; } = string.Empty;
    public Guid TargetFormId { get; set; }
    public FormDefinition? TargetForm { get; set; }
    public Guid TargetRecordId { get; set; }
    public FormRecord? TargetRecord { get; set; }
}
