using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class FormDefinition : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = FormStatuses.Draft;

    public Guid? CurrentVersionId { get; set; }

    public FormVersion? CurrentVersion { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<FormVersion> Versions { get; } = new List<FormVersion>();
}

public static class FormStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
}
