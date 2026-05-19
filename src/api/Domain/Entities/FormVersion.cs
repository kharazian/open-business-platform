using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class FormVersion : CreationAuditedEntity<Guid>
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public int VersionNumber { get; set; }

    public JsonDocument SchemaJson { get; set; } = null!;

    public JsonDocument? LayoutJson { get; set; }

    public JsonDocument? ValidationJson { get; set; }

    public Guid? PublishedById { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
}
