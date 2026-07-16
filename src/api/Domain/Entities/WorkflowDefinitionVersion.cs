using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkflowDefinitionVersion : WorkspaceCreationAuditedEntity<Guid>
{
    public Guid WorkflowDefinitionId { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public int VersionNumber { get; set; }

    public JsonDocument ConfigJson { get; set; } = null!;

    public Guid? PublishedById { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
}
