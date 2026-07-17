using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class AccessPolicy : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public JsonDocument ConditionsJson { get; set; } = JsonSerializer.SerializeToDocument(new { });
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}
