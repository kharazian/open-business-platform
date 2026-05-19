using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class AuditLogEntry : Entity<Guid>, IHasCreationTime
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public JsonDocument? BeforeJson { get; set; }

    public JsonDocument? AfterJson { get; set; }

    public JsonDocument? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
