using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ListReportConfigValidator
{
    private const int SupportedSchemaVersion = 1;

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

            ValidateKnownField(fieldId, validFields, $"{path}.fieldId", errors);

            if (!ReportFilterOperators.Supported.Contains(filter.Operator))
            {
                errors.Add(new ReportValidationError($"{path}.operator", "report.filter.operator", "Filter operator is not supported."));
            }

            if (RequiresFilterValue(filter.Operator) && string.IsNullOrWhiteSpace(filter.Value))
            {
                errors.Add(new ReportValidationError($"{path}.value", "report.filter.value", "Filter value is required for this operator."));
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
        return filterOperator == ReportFilterOperators.Equal || filterOperator == ReportFilterOperators.Contains;
    }
}
