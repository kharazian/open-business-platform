using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerWebhookIdempotency
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string CreateKey(Guid triggerId, string actionId, TriggerEventContext context)
    {
        var material = JsonSerializer.Serialize(new WebhookIdempotencyMaterial(
            triggerId,
            actionId,
            context.EventName,
            context.FormId,
            context.RecordId,
            context.OccurredAt.ToUniversalTime()), JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return $"obp_trigger_{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static bool IsReservedHeader(string? headerName)
    {
        return string.Equals(headerName?.Trim(), HeaderName, StringComparison.OrdinalIgnoreCase);
    }

    public static void ApplyHeaders(
        HttpRequestMessage request,
        IReadOnlyDictionary<string, string>? configuredHeaders,
        string idempotencyKey)
    {
        foreach (var header in configuredHeaders ?? new Dictionary<string, string>())
        {
            if (!IsReservedHeader(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        request.Headers.Remove(HeaderName);
        request.Headers.TryAddWithoutValidation(HeaderName, idempotencyKey);
    }

    private sealed record WebhookIdempotencyMaterial(
        Guid TriggerId,
        string ActionId,
        string EventName,
        Guid FormId,
        Guid RecordId,
        DateTimeOffset OccurredAt);
}
