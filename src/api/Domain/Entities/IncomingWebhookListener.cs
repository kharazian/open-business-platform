using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class IncomingWebhookListener : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public string ListenerKey { get; set; } = string.Empty;

    public Guid TargetFormId { get; set; }

    public FormDefinition? TargetForm { get; set; }

    public string Action { get; set; } = string.Empty;

    public string AuthMode { get; set; } = string.Empty;

    public string? SecretPrefix { get; set; }

    public string? SecretHash { get; set; }

    public string? SafeLookupFieldId { get; set; }

    public JsonDocument MappingJson { get; set; } = JsonSerializer.SerializeToDocument(new { fieldMappings = Array.Empty<object>() });

    public bool IsActive { get; set; } = true;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
