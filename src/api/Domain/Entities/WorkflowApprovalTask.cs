using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkflowApprovalTask : AuditedEntity<Guid>
{
    public Guid ApprovalGroupId { get; set; }

    public Guid WorkflowDefinitionId { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid WorkflowDefinitionVersionId { get; set; }

    public WorkflowDefinitionVersion? WorkflowDefinitionVersion { get; set; }

    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public Guid RecordId { get; set; }

    public FormRecord? Record { get; set; }

    public string ApprovalStepKey { get; set; } = string.Empty;

    public string ApprovalStepName { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string TransitionKey { get; set; } = string.Empty;

    public string TransitionName { get; set; } = string.Empty;

    public string FromStateKey { get; set; } = string.Empty;

    public string ToStateKey { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public Guid AssignedToUserId { get; set; }

    public User? AssignedToUser { get; set; }

    public Guid? RequestedById { get; set; }

    public User? RequestedBy { get; set; }

    public Guid? RespondedById { get; set; }

    public User? RespondedBy { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }

    public string? Comment { get; set; }
}
