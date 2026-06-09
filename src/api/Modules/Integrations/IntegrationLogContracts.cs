namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IntegrationLogDirections
{
    public const string Inbound = "inbound";
    public const string Outbound = "outbound";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Inbound,
        Outbound
    };
}

public static class IntegrationLogTypes
{
    public const string Api = "api";
    public const string Webhook = "webhook";
    public const string Import = "import";
    public const string Export = "export";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Api,
        Webhook,
        Import,
        Export
    };
}

public static class IntegrationLogStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Canceled
    };
}

public sealed record RecordIntegrationLogRequest(
    string Direction,
    string IntegrationType,
    string IntegrationKey,
    string Status,
    string SourceType,
    Guid? SourceId = null,
    string? TargetEntityType = null,
    Guid? TargetEntityId = null,
    int AttemptCount = 0,
    int MaxAttempts = 3,
    bool IsRetryable = false,
    DateTimeOffset? RetryNextAttemptAt = null,
    IReadOnlyDictionary<string, object?>? RequestMetadata = null,
    IReadOnlyDictionary<string, object?>? ResponseMetadata = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null);

public sealed record RequestIntegrationRetryRequest(
    string? ConcurrencyStamp = null,
    DateTimeOffset? RetryNextAttemptAt = null);

public sealed record IntegrationLogDto(
    Guid Id,
    string Direction,
    string IntegrationType,
    string IntegrationKey,
    string SourceType,
    Guid? SourceId,
    string? TargetEntityType,
    Guid? TargetEntityId,
    string Status,
    int AttemptCount,
    int MaxAttempts,
    bool IsRetryable,
    DateTimeOffset? RetryNextAttemptAt,
    DateTimeOffset? RetryLockedAt,
    DateTimeOffset? RetryCompletedAt,
    DateTimeOffset? RetryExhaustedAt,
    DateTimeOffset? RetryRequestedAt,
    Guid? RetryRequestedById,
    IReadOnlyDictionary<string, object?>? RequestMetadata,
    IReadOnlyDictionary<string, object?>? ResponseMetadata,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById)
{
    public string? RetryState => IntegrationRetryStateResolver.Resolve(this);
}

public sealed record IntegrationLogErrorResponse(string Message);
