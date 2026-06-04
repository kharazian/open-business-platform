using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class FormRecord : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public Guid FormVersionId { get; set; }

    public FormVersion? FormVersion { get; set; }

    public string Status { get; set; } = RecordStatuses.Active;

    public Guid? OwnerId { get; set; }

    public User? Owner { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public User? AssignedToUser { get; set; }

    public Guid? AssignedGroupId { get; set; }

    public Group? AssignedGroup { get; set; }

    public Guid? WorkflowDefinitionId { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid? WorkflowDefinitionVersionId { get; set; }

    public WorkflowDefinitionVersion? WorkflowDefinitionVersion { get; set; }

    public string? WorkflowStateKey { get; set; }

    public JsonDocument ValuesJson { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public static class RecordStatuses
{
    public const string Active = "active";
    public const string Deleted = "deleted";
}
