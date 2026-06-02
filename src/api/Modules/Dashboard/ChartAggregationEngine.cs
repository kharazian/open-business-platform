using System.Globalization;
using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class ChartAggregationEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ChartWidgetPreviewDto Execute(
        Guid formId,
        string formName,
        ChartWidgetConfigDefinition config,
        FormSchemaDefinition schema,
        IReadOnlyCollection<FormRecord> records,
        ListReportConfigDefinition? sourceReportConfig = null,
        IReadOnlySet<string>? hiddenFieldIds = null)
    {
        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema)
            .Where(pair => hiddenFieldIds is null || !hiddenFieldIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var preparedRecords = records
            .Select(record => new PreparedChartRecord(record, DeserializeValues(record.ValuesJson)))
            .Where(record => MatchesSourceReportFilters(record, sourceReportConfig))
            .ToArray();
        var normalizedConfig = NormalizeConfig(config);

        return normalizedConfig.WidgetType switch
        {
            ChartWidgetTypes.NumberCard => ToSeriesPreview(formId, formName, normalizedConfig, preparedRecords, [new ChartSeriesPointDto("value", GetMetricLabel(normalizedConfig, fieldsById), Aggregate(preparedRecords, normalizedConfig.Metric))]),
            ChartWidgetTypes.BarChart or ChartWidgetTypes.ChoiceBreakdown => ToSeriesPreview(formId, formName, normalizedConfig, preparedRecords, BuildGroupedSeries(preparedRecords, normalizedConfig, fieldsById)),
            ChartWidgetTypes.DateTrend => ToSeriesPreview(formId, formName, normalizedConfig, preparedRecords, BuildDateTrendSeries(preparedRecords, normalizedConfig)),
            ChartWidgetTypes.Table => ToTablePreview(formId, formName, normalizedConfig, preparedRecords, fieldsById),
            _ => ToSeriesPreview(formId, formName, normalizedConfig, preparedRecords, Array.Empty<ChartSeriesPointDto>())
        };
    }

    private static ChartWidgetConfigDefinition NormalizeConfig(ChartWidgetConfigDefinition config)
    {
        return config with
        {
            WidgetType = config.WidgetType.Trim(),
            Metric = config.Metric is null
                ? new ChartMetricDefinition(ChartMetricTypes.Count)
                : new ChartMetricDefinition(config.Metric.Type.Trim(), NormalizeOptional(config.Metric.FieldId)),
            GroupByFieldId = NormalizeOptional(config.GroupByFieldId),
            DateFieldId = NormalizeOptional(config.DateFieldId),
            Columns = (config.Columns ?? Array.Empty<string>())
                .Select(column => column.Trim())
                .Where(column => column.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            Limit = Math.Clamp(config.Limit ?? 10, 1, 50)
        };
    }

    private static ChartWidgetPreviewDto ToSeriesPreview(
        Guid formId,
        string formName,
        ChartWidgetConfigDefinition config,
        IReadOnlyCollection<PreparedChartRecord> records,
        IReadOnlyList<ChartSeriesPointDto> series)
    {
        return new ChartWidgetPreviewDto(
            formId,
            formName,
            config.WidgetType,
            config.Metric,
            Array.Empty<ChartTableColumnDto>(),
            series,
            Array.Empty<ChartTableRowDto>(),
            records.LongCount());
    }

    private static ChartWidgetPreviewDto ToTablePreview(
        Guid formId,
        string formName,
        ChartWidgetConfigDefinition config,
        IReadOnlyCollection<PreparedChartRecord> records,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        var columnIds = GetTableColumnIds(config, fieldsById);
        var columns = columnIds
            .Select(fieldId => fieldsById[fieldId])
            .Select(field => new ChartTableColumnDto(field.Id, field.Label, field.Type, field.Source))
            .ToArray();
        var rows = records
            .OrderByDescending(record => record.Record.CreatedAt)
            .ThenByDescending(record => record.Record.Id)
            .Take(config.Limit ?? 10)
            .Select(record => ToTableRow(record, columnIds, fieldsById))
            .ToArray();

        return new ChartWidgetPreviewDto(
            formId,
            formName,
            config.WidgetType,
            config.Metric,
            columns,
            Array.Empty<ChartSeriesPointDto>(),
            rows,
            records.LongCount());
    }

    private static IReadOnlyList<string> GetTableColumnIds(
        ChartWidgetConfigDefinition config,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        var requestedColumns = (config.Columns ?? Array.Empty<string>())
            .Where(fieldsById.ContainsKey)
            .ToArray();

        return requestedColumns.Length > 0
            ? requestedColumns
            : fieldsById.Values.Take(5).Select(field => field.Id).ToArray();
    }

    private static ChartTableRowDto ToTableRow(
        PreparedChartRecord record,
        IReadOnlyList<string> columnIds,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        var cells = columnIds.ToDictionary(
            fieldId => fieldId,
            fieldId =>
            {
                var value = GetFieldValue(record, fieldId);
                var metadata = fieldsById[fieldId];
                return new ChartTableCellDto(ToSerializableValue(value), ToDisplayValue(value, metadata));
            },
            StringComparer.Ordinal);

        return new ChartTableRowDto(record.Record.Id, record.Record.Status, cells, record.Record.CreatedAt);
    }

    private static IReadOnlyList<ChartSeriesPointDto> BuildGroupedSeries(
        IReadOnlyCollection<PreparedChartRecord> records,
        ChartWidgetConfigDefinition config,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        var fieldId = config.GroupByFieldId ?? string.Empty;
        var metadata = fieldsById[fieldId];

        return records
            .GroupBy(record => ToGroupKey(GetFieldValue(record, fieldId)), StringComparer.Ordinal)
            .Select(group => new ChartSeriesPointDto(
                group.Key,
                ToGroupLabel(group.Key, metadata),
                Aggregate(group, config.Metric)))
            .OrderByDescending(point => point.Value)
            .ThenBy(point => point.Label)
            .Take(config.Limit ?? 10)
            .ToArray();
    }

    private static IReadOnlyList<ChartSeriesPointDto> BuildDateTrendSeries(
        IReadOnlyCollection<PreparedChartRecord> records,
        ChartWidgetConfigDefinition config)
    {
        var fieldId = config.DateFieldId ?? string.Empty;

        return records
            .Select(record => new
            {
                Record = record,
                Date = TryConvertDateTime(GetFieldValue(record, fieldId), out var dateTime) ? dateTime.Date : (DateTime?)null
            })
            .Where(item => item.Date is not null)
            .GroupBy(item => item.Date!.Value)
            .OrderBy(group => group.Key)
            .TakeLast(config.Limit ?? 10)
            .Select(group => new ChartSeriesPointDto(
                group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                group.Key.ToString("MMM d", CultureInfo.InvariantCulture),
                Aggregate(group.Select(item => item.Record), config.Metric)))
            .ToArray();
    }

    private static decimal Aggregate(IEnumerable<PreparedChartRecord> records, ChartMetricDefinition metric)
    {
        var materializedRecords = records.ToArray();

        return metric.Type switch
        {
            ChartMetricTypes.Sum => materializedRecords.Sum(record => TryConvertDecimal(GetFieldValue(record, metric.FieldId ?? string.Empty), out var value) ? value : 0m),
            ChartMetricTypes.Average => Average(materializedRecords, metric.FieldId ?? string.Empty),
            _ => materializedRecords.Length
        };
    }

    private static decimal Average(IReadOnlyCollection<PreparedChartRecord> records, string fieldId)
    {
        var values = records
            .Select(record => TryConvertDecimal(GetFieldValue(record, fieldId), out var value) ? value : (decimal?)null)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToArray();

        return values.Length == 0 ? 0m : values.Average();
    }

    private static bool MatchesSourceReportFilters(PreparedChartRecord record, ListReportConfigDefinition? sourceReportConfig)
    {
        return sourceReportConfig?.Filters is null || sourceReportConfig.Filters.All(filter => MatchesFilter(record, filter));
    }

    private static bool MatchesFilter(PreparedChartRecord record, ListReportFilterDefinition filter)
    {
        var value = GetFieldValue(record, filter.FieldId.Trim());
        var filterValue = NormalizeOptional(filter.Value);

        return filter.Operator.Trim() switch
        {
            ReportFilterOperators.Equal => string.Equals(ToSearchText(value), filterValue, StringComparison.OrdinalIgnoreCase),
            ReportFilterOperators.Contains => filterValue is not null && ToSearchText(value).Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            ReportFilterOperators.IsEmpty => IsEmptyValue(value),
            ReportFilterOperators.IsNotEmpty => !IsEmptyValue(value),
            _ => true
        };
    }

    private static object? GetFieldValue(PreparedChartRecord record, string fieldId)
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

    private static string ToDisplayValue(object? value, ReportableFieldMetadata metadata)
    {
        var text = ToSearchText(value);
        var option = metadata.Options.FirstOrDefault(candidate => string.Equals(candidate.Value, text, StringComparison.Ordinal));

        if (option is not null)
        {
            return option.Label;
        }

        if (metadata.Id == ReportableSystemFields.Status && !string.IsNullOrWhiteSpace(text))
        {
            return ToTitle(text);
        }

        return value switch
        {
            null => string.Empty,
            DateTimeOffset dateTime => dateTime.ToString("u", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            bool boolean => boolean ? "Yes" : "No",
            _ => text
        };
    }

    private static string ToGroupKey(object? value)
    {
        return ToSearchText(value).Trim();
    }

    private static string ToGroupLabel(string key, ReportableFieldMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "(blank)";
        }

        var option = metadata.Options.FirstOrDefault(candidate => string.Equals(candidate.Value, key, StringComparison.Ordinal));

        if (option is not null)
        {
            return option.Label;
        }

        return metadata.Id == ReportableSystemFields.Status ? ToTitle(key) : key;
    }

    private static string GetMetricLabel(ChartWidgetConfigDefinition config, IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById)
    {
        if (config.Metric.Type == ChartMetricTypes.Count)
        {
            return "Records";
        }

        return config.Metric.FieldId is not null && fieldsById.TryGetValue(config.Metric.FieldId, out var field)
            ? field.Label
            : "Value";
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

    private static bool TryConvertDecimal(object? value, out decimal number)
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

    private static bool TryConvertDateTime(object? value, out DateTime dateTime)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffset:
                dateTime = dateTimeOffset.UtcDateTime;
                return true;
            case DateTime dateTimeValue:
                dateTime = dateTimeValue;
                return true;
            case string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed):
                dateTime = parsed;
                return true;
            default:
                dateTime = default;
                return false;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string ToTitle(string value)
    {
        var words = value.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length == 0
            ? value
            : string.Join(" ", words.Select(word => string.Concat(char.ToUpperInvariant(word[0]).ToString(), word[1..].ToLowerInvariant())));
    }

    private sealed record PreparedChartRecord(FormRecord Record, IReadOnlyDictionary<string, object?> Values);
}
