using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class IntegrationApiKey : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public string IntegrationKey { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public JsonDocument ScopesJson { get; set; } = JsonSerializer.SerializeToDocument(Array.Empty<string>());

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastUsedAt { get; set; }

    public string? LastUsedIp { get; set; }

    public string? LastUsedUserAgent { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedById { get; set; }

    public User? RevokedBy { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
