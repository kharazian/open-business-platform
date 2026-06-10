using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IncomingWebhookPayloadMapper
{
    public static IReadOnlyDictionary<string, object?> MapValues(
        IncomingWebhookMappingDefinition mapping,
        IReadOnlyDictionary<string, object?> payload)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var fieldMapping in mapping.FieldMappings)
        {
            if (TryResolvePath(payload, fieldMapping.SourcePath, out var value))
            {
                values[fieldMapping.TargetFieldId] = value;
            }
            else if (fieldMapping.Required)
            {
                values[fieldMapping.TargetFieldId] = null;
            }
        }

        return values;
    }

    private static bool TryResolvePath(IReadOnlyDictionary<string, object?> payload, string sourcePath, out object? value)
    {
        object? current = payload;

        foreach (var segment in sourcePath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is JsonElement jsonElement)
            {
                current = JsonElementToObject(jsonElement);
            }

            if (current is IReadOnlyDictionary<string, object?> dictionary
                && dictionary.TryGetValue(segment, out var next))
            {
                current = next;
                continue;
            }

            if (current is IDictionary<string, object?> mutableDictionary
                && mutableDictionary.TryGetValue(segment, out next))
            {
                current = next;
                continue;
            }

            value = null;
            return false;
        }

        value = current is JsonElement element ? JsonElementToObject(element) : current;
        return true;
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => JsonElementToObject(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            _ => element.ToString()
        };
    }
}
