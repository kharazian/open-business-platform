using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerEventOutboxStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string DeadLetter = "dead_letter";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Processing,
        Completed,
        DeadLetter
    };
}

public sealed class TriggerEventOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int DefaultMaxAttempts = 5;
    private readonly OpenBusinessPlatformDbContext dbContext;

    public TriggerEventOutbox(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public TriggerEventOutboxMessage Enqueue(TriggerEventContext context)
    {
        var message = new TriggerEventOutboxMessage
        {
            Id = Guid.NewGuid(),
            FormId = context.FormId,
            RecordId = context.RecordId,
            EventName = context.EventName,
            PayloadJson = JsonSerializer.SerializeToDocument(context, JsonOptions),
            Status = TriggerEventOutboxStatuses.Pending,
            MaxAttempts = DefaultMaxAttempts,
            NextAttemptAt = DateTimeOffset.UtcNow
        };

        dbContext.TriggerEventOutbox.Add(message);
        return message;
    }
}
