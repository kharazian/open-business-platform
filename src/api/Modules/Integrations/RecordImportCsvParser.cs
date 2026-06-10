namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class RecordImportCsvParser
{
    public static RecordImportCsvDocument Parse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new RecordImportCsvDocument(Array.Empty<string>(), Array.Empty<RecordImportCsvRow>());
        }

        var records = ParseRecords(csvContent);
        if (records.Count == 0)
        {
            return new RecordImportCsvDocument(Array.Empty<string>(), Array.Empty<RecordImportCsvRow>());
        }

        var headers = records[0].Select(header => header.Trim()).ToArray();
        var rows = records
            .Skip(1)
            .Select((record, index) =>
            {
                var values = new Dictionary<string, string?>(StringComparer.Ordinal);
                for (var i = 0; i < headers.Length; i++)
                {
                    values[headers[i]] = i < record.Count ? record[i] : null;
                }

                return new RecordImportCsvRow(index + 2, values);
            })
            .ToArray();

        return new RecordImportCsvDocument(headers, rows);
    }

    private static List<List<string>> ParseRecords(string content)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var character = content[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                record.Add(field.ToString());
                field.Clear();
                continue;
            }

            if ((character == '\n' || character == '\r') && !inQuotes)
            {
                if (character == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                record.Add(field.ToString());
                field.Clear();
                if (record.Any(value => !string.IsNullOrWhiteSpace(value)))
                {
                    records.Add(record);
                }
                record = new List<string>();
                continue;
            }

            field.Append(character);
        }

        record.Add(field.ToString());
        if (record.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            records.Add(record);
        }

        return records;
    }
}
