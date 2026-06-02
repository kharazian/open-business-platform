using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class Notification : Entity<Guid>, IHasCreationTime
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public Guid? SourceId { get; set; }

    public Guid? TriggerId { get; set; }

    public Guid? TriggerLogId { get; set; }

    public string? ActionId { get; set; }

    public JsonDocument? MetadataJson { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
