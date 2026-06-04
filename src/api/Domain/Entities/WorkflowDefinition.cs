using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkflowDefinition : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = WorkflowDefinitionStatuses.Draft;

    public bool IsEnabled { get; set; } = true;

    public bool HasUnpublishedChanges { get; set; }

    public Guid? CurrentVersionId { get; set; }

    public WorkflowDefinitionVersion? CurrentVersion { get; set; }

    public JsonDocument DraftConfigJson { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<WorkflowDefinitionVersion> Versions { get; } = new List<WorkflowDefinitionVersion>();
}

public static class WorkflowDefinitionStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
}
