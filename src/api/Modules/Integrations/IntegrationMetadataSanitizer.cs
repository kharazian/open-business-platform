using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IntegrationMetadataSanitizer
{
    public const string RedactedValue = "[redacted]";

    private static readonly IReadOnlyCollection<string> SensitiveNameParts = new[]
    {
        "authorization",
        "cookie",
        "password",
        "secret",
        "token",
        "api-key",
        "apikey",
        "x-obp-api-key"
    };

    public static IReadOnlyDictionary<string, object?>? Sanitize(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        return metadata.ToDictionary(
            pair => pair.Key,
            pair => SanitizeValue(pair.Key, pair.Value),
            StringComparer.Ordinal);
    }

    private static object? SanitizeValue(string key, object? value)
    {
        if (IsSensitiveName(key))
        {
            return RedactedValue;
        }

        return value switch
        {
            null => null,
            JsonElement jsonElement => SanitizeJsonElement(key, jsonElement),
            IReadOnlyDictionary<string, object?> dictionary => Sanitize(dictionary),
            IDictionary<string, object?> dictionary => Sanitize(dictionary.AsReadOnly()),
            string text => text.Length <= 500 ? text : text[..500],
            _ => value
        };
    }

    private static object? SanitizeJsonElement(string key, JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => SanitizeValue(property.Name, property.Value),
                    StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(item => SanitizeJsonElement(key, item)).ToArray(),
            JsonValueKind.String => SanitizeValue(key, element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static bool IsSensitiveName(string name)
    {
        return SensitiveNameParts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class DictionaryExtensions
{
    public static IReadOnlyDictionary<string, object?> AsReadOnly(this IDictionary<string, object?> dictionary)
    {
        return new Dictionary<string, object?>(dictionary, StringComparer.Ordinal);
    }
}
