using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkflowHistoryEntry : CreationAuditedEntity<Guid>
{
    public Guid WorkflowDefinitionId { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid WorkflowDefinitionVersionId { get; set; }

    public WorkflowDefinitionVersion? WorkflowDefinitionVersion { get; set; }

    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public Guid RecordId { get; set; }

    public FormRecord? Record { get; set; }

    public string? FromStateKey { get; set; }

    public string ToStateKey { get; set; } = string.Empty;

    public string? TransitionKey { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public JsonDocument? MetadataJson { get; set; }
}
