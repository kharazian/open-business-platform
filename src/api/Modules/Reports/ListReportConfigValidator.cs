using System.Globalization;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ListReportConfigValidator
{
    private const int SupportedSchemaVersion = 1;
    private static readonly IReadOnlySet<string> DefaultFilterOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        ReportFilterOperators.Equal,
        ReportFilterOperators.Contains,
        ReportFilterOperators.IsEmpty,
        ReportFilterOperators.IsNotEmpty
    };
    private static readonly IReadOnlySet<string> NumericFilterOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        ReportFilterOperators.Equal,
        ReportFilterOperators.GreaterThan,
        ReportFilterOperators.GreaterOrEqual,
        ReportFilterOperators.LessThan,
        ReportFilterOperators.LessOrEqual,
        ReportFilterOperators.IsEmpty,
        ReportFilterOperators.IsNotEmpty
    };
    private static readonly IReadOnlySet<string> TemporalFilterOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        ReportFilterOperators.Equal,
        ReportFilterOperators.Before,
        ReportFilterOperators.After,
        ReportFilterOperators.IsEmpty,
        ReportFilterOperators.IsNotEmpty
    };
    private static readonly IReadOnlySet<string> ChoiceFilterOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        ReportFilterOperators.Equal,
        ReportFilterOperators.IsEmpty,
        ReportFilterOperators.IsNotEmpty
    };

    public static ReportValidationResult Validate(FormSchemaDefinition schema, ListReportConfigDefinition? config)
    {
        var errors = new List<ReportValidationError>();

        if (config is null)
        {
            errors.Add(new ReportValidationError("config", "report.config.required", "Report config is required."));
            return new ReportValidationResult(errors);
        }

        if (config.SchemaVersion != SupportedSchemaVersion)
        {
            errors.Add(new ReportValidationError("config.schemaVersion", "report.schemaVersion.unsupported", "Report config schema version is not supported."));
        }

        var validFields = FormReportableFieldMetadata.GetReportableFieldsById(schema);

        ValidateColumns(config.Columns, validFields, errors);
        ValidateFilters(config.Filters, validFields, errors);
        ValidateSort(config.Sort, validFields, errors);
        ValidateRowOpenAction(config.RowOpenAction, errors);

        return new ReportValidationResult(errors);
    }

    private static void ValidateColumns(
        IReadOnlyList<ListReportColumnDefinition>? columns,
        IReadOnlyDictionary<string, ReportableFieldMetadata> validFields,
        List<ReportValidationError> errors)
    {
        if (columns is null || columns.Count == 0 || columns.All(column => !column.Visible))
        {
            errors.Add(new ReportValidationError("config.columns", "report.columns.required", "Choose at least one visible column."));
            return;
        }

        var seenFields = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var path = $"config.columns[{index}]";
            var fieldId = column.FieldId.Trim();
            var label = column.Label.Trim();

            ValidateKnownField(fieldId, validFields, $"{path}.fieldId", errors);

            if (!seenFields.Add(fieldId))
            {
                errors.Add(new ReportValidationError($"{path}.fieldId", "report.field.duplicate", "Report fields can only be selected once."));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                errors.Add(new ReportValidationError($"{path}.label", "report.label.required", "Column label is required."));
            }

            if (column.Width is < 80 or > 480)
            {
                errors.Add(new ReportValidationError($"{path}.width", "report.width.range", "Column width must be between 80 and 480 pixels."));
            }
        }
    }

    private static void ValidateFilters(
        IReadOnlyList<ListReportFilterDefinition>? filters,
        IReadOnlyDictionary<string, ReportableFieldMetadata> validFields,
        List<ReportValidationError> errors)
    {
        if (filters is null)
        {
            return;
        }

        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
            var path = $"config.filters[{index}]";
            var fieldId = filter.FieldId.Trim();
            var filterOperator = filter.Operator.Trim();
            validFields.TryGetValue(fieldId, out var field);

            ValidateKnownField(fieldId, validFields, $"{path}.fieldId", errors);

            if (!ReportFilterOperators.Supported.Contains(filterOperator))
            {
                errors.Add(new ReportValidationError($"{path}.operator", "report.filter.operator", "Filter operator is not supported."));
            }
            else if (field is not null && !GetSupportedFilterOperators(field.Type).Contains(filterOperator))
            {
                errors.Add(new ReportValidationError($"{path}.operator", "report.filter.operator_field", "Filter operator is not supported for this field."));
            }

            if (RequiresFilterValue(filterOperator) && string.IsNullOrWhiteSpace(filter.Value))
            {
                errors.Add(new ReportValidationError($"{path}.value", "report.filter.value", "Filter value is required for this operator."));
            }
            else if (field is not null && RequiresFilterValue(filterOperator) && !string.IsNullOrWhiteSpace(filter.Value))
            {
                ValidateFilterValue(field, filter.Value, $"{path}.value", errors);
            }
        }
    }

    private static void ValidateSort(
        IReadOnlyList<ListReportSortDefinition>? sort,
        IReadOnlyDictionary<string, ReportableFieldMetadata> validFields,
        List<ReportValidationError> errors)
    {
        if (sort is null)
        {
            return;
        }

        var seenFields = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < sort.Count; index++)
        {
            var sortItem = sort[index];
            var path = $"config.sort[{index}]";
            var fieldId = sortItem.FieldId.Trim();

            ValidateKnownField(fieldId, validFields, $"{path}.fieldId", errors);

            if (!seenFields.Add(fieldId))
            {
                errors.Add(new ReportValidationError($"{path}.fieldId", "report.sort.duplicate", "Sort fields can only be selected once."));
            }

            if (!ReportSortDirections.Supported.Contains(sortItem.Direction))
            {
                errors.Add(new ReportValidationError($"{path}.direction", "report.sort.direction", "Sort direction is not supported."));
            }
        }
    }

    private static void ValidateKnownField(
        string fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> validFields,
        string path,
        List<ReportValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            errors.Add(new ReportValidationError(path, "report.field.required", "Field is required."));
            return;
        }

        if (!validFields.ContainsKey(fieldId))
        {
            errors.Add(new ReportValidationError(path, "report.field.unknown", "Report field does not exist on this form."));
        }
    }

    private static bool RequiresFilterValue(string filterOperator)
    {
        return filterOperator != ReportFilterOperators.IsEmpty && filterOperator != ReportFilterOperators.IsNotEmpty;
    }

    private static IReadOnlySet<string> GetSupportedFilterOperators(string fieldType)
    {
        if (FormFieldTypes.IsNumeric(fieldType))
        {
            return NumericFilterOperators;
        }

        if (fieldType is FormFieldTypes.Date or FormFieldTypes.Datetime or FormFieldTypes.Time)
        {
            return TemporalFilterOperators;
        }

        if (fieldType is FormFieldTypes.Select or FormFieldTypes.Radio or "status")
        {
            return ChoiceFilterOperators;
        }

        return DefaultFilterOperators;
    }

    private static void ValidateFilterValue(
        ReportableFieldMetadata field,
        string value,
        string path,
        List<ReportValidationError> errors)
    {
        var normalizedValue = value.Trim();

        if (FormFieldTypes.IsNumeric(field.Type)
            && !decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            errors.Add(new ReportValidationError(path, "report.filter.value_number", "Filter value must be a number."));
            return;
        }

        if (string.Equals(field.Type, FormFieldTypes.Time, StringComparison.Ordinal)
            && !IsValidTimeFilterValue(normalizedValue))
        {
            errors.Add(new ReportValidationError(path, "report.filter.value_time", "Filter value must be a valid time."));
            return;
        }

        if ((string.Equals(field.Type, FormFieldTypes.Date, StringComparison.Ordinal)
                || string.Equals(field.Type, FormFieldTypes.Datetime, StringComparison.Ordinal))
            && !DateTimeOffset.TryParse(normalizedValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
        {
            errors.Add(new ReportValidationError(path, "report.filter.value_date", "Filter value must be a valid date/time."));
            return;
        }

        if (field.Options.Count > 0
            && IsChoiceFilterField(field.Type)
            && !field.Options.Any(option => string.Equals(option.Value, normalizedValue, StringComparison.Ordinal)))
        {
            errors.Add(new ReportValidationError(path, "report.filter.value_option", "Choose an available filter value."));
        }
    }

    private static bool IsChoiceFilterField(string fieldType)
    {
        return fieldType is FormFieldTypes.Select or FormFieldTypes.Radio or "status";
    }

    private static bool IsValidTimeFilterValue(string value)
    {
        return TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            || TimeOnly.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static void ValidateRowOpenAction(string? rowOpenAction, List<ReportValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(rowOpenAction))
        {
            return;
        }

        if (!ListReportRowOpenActions.Supported.Contains(rowOpenAction.Trim()))
        {
            errors.Add(new ReportValidationError("config.rowOpenAction", "report.row_open_action", "Row open action is not supported."));
        }
    }
}
