using System.Globalization;
using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class FormAddressValueFormatter
{
    private static readonly string[] TextSubfields =
    [
        FormAddressSubfields.Line1, FormAddressSubfields.Line2, FormAddressSubfields.City,
        FormAddressSubfields.Region, FormAddressSubfields.PostalCode, FormAddressSubfields.Country
    ];

    public static bool TryFormat(object? value, out string displayValue)
    {
        if (!TryGetObject(value, out var address))
        {
            displayValue = string.Empty;
            return false;
        }

        var parts = TextSubfields
            .Select(subfield => address.TryGetProperty(subfield, out var member) && member.ValueKind == JsonValueKind.String ? member.GetString()?.Trim() : null)
            .Where(member => !string.IsNullOrWhiteSpace(member))
            .ToArray();
        if (parts.Length > 0)
        {
            displayValue = string.Join(", ", parts!);
            return true;
        }

        if (TryGetCoordinate(address, FormAddressSubfields.Latitude, out var latitude)
            && TryGetCoordinate(address, FormAddressSubfields.Longitude, out var longitude))
        {
            displayValue = $"{latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}";
            return true;
        }

        displayValue = string.Empty;
        return true;
    }

    private static bool TryGetObject(object? value, out JsonElement address)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.Object } element)
        {
            address = element;
            return true;
        }
        if (value is not null && value is not string && value is not ValueType)
        {
            var serialized = JsonSerializer.SerializeToElement(value);
            if (serialized.ValueKind == JsonValueKind.Object)
            {
                address = serialized;
                return true;
            }
        }
        address = default;
        return false;
    }

    private static bool TryGetCoordinate(JsonElement address, string name, out decimal coordinate)
    {
        coordinate = default;
        return address.TryGetProperty(name, out var member)
            && member.ValueKind == JsonValueKind.Number
            && member.TryGetDecimal(out coordinate);
    }
}
