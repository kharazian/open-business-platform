using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IncomingWebhookListenerAuthModes
{
    public const string ApiKey = "api_key";
    public const string ListenerSecret = "listener_secret";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ApiKey,
        ListenerSecret
    };
}

public static class IncomingWebhookListenerActions
{
    public const string Create = "create";
    public const string Upsert = "upsert";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Create,
        Upsert
    };
}

public sealed record IncomingWebhookFieldMappingDefinition(
    string SourcePath,
    string TargetFieldId,
    bool Required = false);

public sealed record IncomingWebhookMappingDefinition(
    IReadOnlyList<IncomingWebhookFieldMappingDefinition> FieldMappings);

public sealed record UpsertIncomingWebhookListenerRequest(
    string Name,
    string ListenerKey,
    Guid TargetFormId,
    string Action,
    string AuthMode,
    IncomingWebhookMappingDefinition Mapping,
    bool IsActive = false,
    string? SafeLookupFieldId = null);

public sealed record IncomingWebhookListenerDto(
    Guid Id,
    string Name,
    string ListenerKey,
    Guid TargetFormId,
    string Action,
    string AuthMode,
    string? SecretPrefix,
    string? SafeLookupFieldId,
    IncomingWebhookMappingDefinition Mapping,
    bool IsActive,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record IncomingWebhookListenerSecretDto(
    IncomingWebhookListenerDto Listener,
    string RawSecret);

public sealed record ReceiveIncomingWebhookResponse(
    string Status,
    Guid? RecordId,
    Guid IntegrationLogId);

public sealed record IncomingWebhookValidationResult(IReadOnlyList<IncomingWebhookValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record IncomingWebhookValidationError(string Path, string Code, string Message);

public sealed class IncomingWebhookException : Exception
{
    public IncomingWebhookException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public static class IncomingWebhookJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
