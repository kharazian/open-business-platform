using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class IntegrationConnector : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public JsonDocument ConfigJson { get; set; } = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>());

    public JsonDocument SecretMetadataJson { get; set; } = JsonSerializer.SerializeToDocument(Array.Empty<string>());

    public bool IsActive { get; set; } = true;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
