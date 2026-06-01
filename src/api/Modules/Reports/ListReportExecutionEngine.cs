using System.Globalization;
using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ListReportExecutionEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ListReportExecutionDto Execute(
        Guid reportId,
        Guid formId,
        string reportName,
        string formName,
        ListReportConfigDefinition config,
        FormSchemaDefinition schema,
        IReadOnlyCollection<FormRecord> records,
        RunListReportRequest request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var preparedReport = PrepareReport(config, schema, records, request.Search);
        var pageRecords = preparedReport.Records
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(record => ToRowDto(record, preparedReport.Columns))
            .ToArray();

        return new ListReportExecutionDto(
            reportId,
            formId,
            reportName,
            formName,
            page,
            pageSize,
            preparedReport.Records.LongLength,
            preparedReport.Columns,
            pageRecords);
    }

    public static ListReportExecutionDto ExecuteAll(
        Guid reportId,
        Guid formId,
        string reportName,
        string formName,
        ListReportConfigDefinition config,
        FormSchemaDefinition schema,
        IReadOnlyCollection<FormRecord> records,
        string? search = null)
    {
        var preparedReport = PrepareReport(config, schema, records, search);
        var rows = preparedReport.Records
            .Select(record => ToRowDto(record, preparedReport.Columns))
            .ToArray();

        return new ListReportExecutionDto(
            reportId,
            formId,
            reportName,
            formName,
            1,
            Math.Max(1, rows.Length),
            preparedReport.Records.LongLength,
            preparedReport.Columns,
            rows);
    }

    private static PreparedReport PrepareReport(
        ListReportConfigDefinition config,
        FormSchemaDefinition schema,
        IReadOnlyCollection<FormRecord> records,
        string? search)
    {
        var normalizedSearch = Normalize(search);
        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema);
        var columns = GetVisibleColumns(config, fieldsById);
        var preparedRecords = records
            .Select(record => new PreparedReportRecord(record, DeserializeValues(record.ValuesJson)))
            .Where(record => MatchesFilters(record, config.Filters))
            .Where(record => MatchesSearch(record, columns, fieldsById, normalizedSearch))
            .ToArray();

        return new PreparedReport(columns, SortRecords(preparedRecords, config.Sort));
    }

    private static IReadOnlyList<ListReportExecutionColumnDto> GetVisibleColumns(
        ListReportConfigDefinition config,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        return (config.Columns ?? Array.Empty<ListReportColumnDefinition>())
            .Where(column => column.Visible)
            .Select(column => new
            {
                FieldId = column.FieldId.Trim(),
                Label = column.Label.Trim(),
                column.Width
            })
            .Where(column => fieldsById.ContainsKey(column.FieldId))
            .Select(column =>
            {
                var metadata = fieldsById[column.FieldId];
                return new ListReportExecutionColumnDto(
                    column.FieldId,
                    string.IsNullOrWhiteSpace(column.Label) ? metadata.Label : column.Label,
                    metadata.Type,
                    metadata.Source,
                    column.Width);
            })
            .ToArray();
    }

    private static bool MatchesFilters(PreparedReportRecord record, IReadOnlyList<ListReportFilterDefinition>? filters)
    {
        return filters is null || filters.All(filter => MatchesFilter(record, filter));
    }

    private static bool MatchesFilter(PreparedReportRecord record, ListReportFilterDefinition filter)
    {
        var value = GetFieldValue(record, filter.FieldId.Trim());
        var filterValue = Normalize(filter.Value);

        return filter.Operator.Trim() switch
        {
            ReportFilterOperators.Equal => string.Equals(ToSearchText(value), filterValue, StringComparison.OrdinalIgnoreCase),
            ReportFilterOperators.Contains => filterValue is not null && ToSearchText(value).Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            ReportFilterOperators.IsEmpty => IsEmptyValue(value),
            ReportFilterOperators.IsNotEmpty => !IsEmptyValue(value),
            _ => true
        };
    }

    private static bool MatchesSearch(
        PreparedReportRecord record,
        IReadOnlyList<ListReportExecutionColumnDto> columns,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        string? search)
    {
        if (search is null)
        {
            return true;
        }

        if (record.Record.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return columns.Any(column =>
        {
            if (!fieldsById.TryGetValue(column.FieldId, out var metadata) || !metadata.Searchable)
            {
                return false;
            }

            return ToSearchText(GetFieldValue(record, column.FieldId)).Contains(search, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static PreparedReportRecord[] SortRecords(
        IReadOnlyCollection<PreparedReportRecord> records,
        IReadOnlyList<ListReportSortDefinition>? sort)
    {
        var sorted = records.ToArray();

        if (sort is null || sort.Count == 0)
        {
            return sorted
                .OrderByDescending(record => record.Record.CreatedAt)
                .ThenByDescending(record => record.Record.Id)
                .ToArray();
        }

        foreach (var sortItem in sort.Reverse())
        {
            var fieldId = sortItem.FieldId.Trim();
            sorted = sortItem.Direction.Trim() == ReportSortDirections.Desc
                ? sorted.OrderByDescending(record => ToSortValue(GetFieldValue(record, fieldId)), ReportSortValueComparer.Instance).ToArray()
                : sorted.OrderBy(record => ToSortValue(GetFieldValue(record, fieldId)), ReportSortValueComparer.Instance).ToArray();
        }

        return sorted;
    }

    private static ListReportExecutionRowDto ToRowDto(
        PreparedReportRecord record,
        IReadOnlyList<ListReportExecutionColumnDto> columns)
    {
        var cells = columns.ToDictionary(
            column => column.FieldId,
            column =>
            {
                var value = GetFieldValue(record, column.FieldId);
                return new ListReportExecutionCellDto(ToSerializableValue(value), ToDisplayValue(value));
            },
            StringComparer.Ordinal);

        return new ListReportExecutionRowDto(record.Record.Id, record.Record.Status, cells, record.Record.CreatedAt);
    }

    private static object? GetFieldValue(PreparedReportRecord record, string fieldId)
    {
        return fieldId switch
        {
            ReportableSystemFields.Status => record.Record.Status,
            ReportableSystemFields.CreatedAt => record.Record.CreatedAt,
            ReportableSystemFields.CreatedById => record.Record.CreatedById,
            ReportableSystemFields.UpdatedAt => record.Record.UpdatedAt,
            ReportableSystemFields.UpdatedById => record.Record.UpdatedById,
            ReportableSystemFields.OwnerId => record.Record.OwnerId,
            ReportableSystemFields.DepartmentId => record.Record.DepartmentId,
            _ => record.Values.TryGetValue(fieldId, out var value) ? value : null
        };
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        var rawValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        return rawValues.ToDictionary(pair => pair.Key, pair => ConvertJsonValue(pair.Value), StringComparer.Ordinal);
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
    }

    private static object? ToSerializableValue(object? value)
    {
        return value switch
        {
            null => null,
            DateTimeOffset dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            _ => value
        };
    }

    private static string ToDisplayValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTimeOffset dateTime => dateTime.ToString("u", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            bool boolean => boolean ? "Yes" : "No",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string ToSearchText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTimeOffset dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static bool IsEmptyValue(object? value)
    {
        return value is null || value is string text && string.IsNullOrWhiteSpace(text);
    }

    private static ReportSortValue ToSortValue(object? value)
    {
        if (value is null)
        {
            return ReportSortValue.Empty;
        }

        if (TryConvertDecimal(value, out var number))
        {
            return new ReportSortValue(false, number, null, ToSearchText(value));
        }

        if (TryConvertDateTime(value, out var dateTime))
        {
            return new ReportSortValue(false, null, dateTime, ToSearchText(value));
        }

        return new ReportSortValue(false, null, null, ToSearchText(value));
    }

    private static bool TryConvertDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case decimal decimalValue:
                number = decimalValue;
                return true;
            case int intValue:
                number = intValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case double doubleValue:
                number = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                return true;
            case string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static bool TryConvertDateTime(object value, out DateTimeOffset dateTime)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffset:
                dateTime = dateTimeOffset;
                return true;
            case DateTime dateTimeValue:
                dateTime = new DateTimeOffset(dateTimeValue);
                return true;
            case string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed):
                dateTime = parsed;
                return true;
            default:
                dateTime = default;
                return false;
        }
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record PreparedReportRecord(FormRecord Record, IReadOnlyDictionary<string, object?> Values);

    private sealed record PreparedReport(IReadOnlyList<ListReportExecutionColumnDto> Columns, PreparedReportRecord[] Records);

    private sealed record ReportSortValue(bool IsEmpty, decimal? Number, DateTimeOffset? DateTime, string Text)
    {
        public static ReportSortValue Empty { get; } = new(true, null, null, string.Empty);
    }

    private sealed class ReportSortValueComparer : IComparer<ReportSortValue>
    {
        public static ReportSortValueComparer Instance { get; } = new();

        public int Compare(ReportSortValue? x, ReportSortValue? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null || x.IsEmpty)
            {
                return y is null || y.IsEmpty ? 0 : -1;
            }

            if (y is null || y.IsEmpty)
            {
                return 1;
            }

            if (x.Number.HasValue && y.Number.HasValue)
            {
                return x.Number.Value.CompareTo(y.Number.Value);
            }

            if (x.DateTime.HasValue && y.DateTime.HasValue)
            {
                return x.DateTime.Value.CompareTo(y.DateTime.Value);
            }

            return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
