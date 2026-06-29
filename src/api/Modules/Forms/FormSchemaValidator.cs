using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public static partial class FormSchemaValidator
{
    private static readonly Regex EmailPattern = CreateEmailPattern();
    private static readonly Regex DatePattern = CreateDatePattern();
    private static readonly Regex TimePattern = CreateTimePattern();
    private static readonly Regex DatetimePattern = CreateDatetimePattern();

    public static FormValidationResult ValidateSchema(FormSchemaDefinition? schema)
    {
        return ValidateSchemaCore(schema, requireFields: true, requireLayoutRows: true, requireFieldConfiguration: true);
    }

    public static FormValidationResult ValidateDraftSchema(FormSchemaDefinition? schema)
    {
        return ValidateSchemaCore(schema, requireFields: false, requireLayoutRows: false, requireFieldConfiguration: false);
    }

    private static FormValidationResult ValidateSchemaCore(
        FormSchemaDefinition? schema,
        bool requireFields,
        bool requireLayoutRows,
        bool requireFieldConfiguration)
    {
        var errors = new List<FormValidationError>();
        var fieldIds = new HashSet<string>(StringComparer.Ordinal);

        if (schema is null)
        {
            errors.Add(Error("", "schema.required", "Form schema is required."));
            return new FormValidationResult(errors);
        }

        if (schema.SchemaVersion != 1)
        {
            errors.Add(Error("schemaVersion", "schema.version", "Schema version must be 1."));
        }

        if (schema.Fields is null)
        {
            errors.Add(Error("fields", "fields.required", "Fields must be an array."));
        }
        else if (requireFields && schema.Fields.Count == 0)
        {
            errors.Add(Error("fields", "fields.required", "At least one field is required."));
        }
        else
        {
            for (var index = 0; index < schema.Fields.Count; index++)
            {
                ValidateField(schema.Fields[index], index, fieldIds, errors, requireFieldConfiguration);
            }
        }

        ValidateLayout(schema.Layout, fieldIds, errors, requireLayoutRows);

        return new FormValidationResult(errors);
    }

    public static FormValidationResult ValidateRecordValues(
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<FormValidationError>();
        var fieldsById = schema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);

        foreach (var valueKey in values.Keys)
        {
            if (!fieldsById.ContainsKey(valueKey))
            {
                errors.Add(Error($"values.{valueKey}", "record.field_unknown", $"Record contains unknown field '{valueKey}'."));
            }
        }

        foreach (var field in schema.Fields)
        {
            var hasValue = values.TryGetValue(field.Id, out var value);

            if (field.Required && (!hasValue || IsEmptyValue(value)))
            {
                errors.Add(Error($"values.{field.Id}", "record.required", $"'{field.Label}' is required."));
                continue;
            }

            if (!hasValue || IsEmptyValue(value))
            {
                continue;
            }

            ValidateRecordFieldValue(field, value, errors);
        }

        return new FormValidationResult(errors);
    }

    private static void ValidateField(
        FormFieldDefinition field,
        int index,
        HashSet<string> fieldIds,
        List<FormValidationError> errors,
        bool requireFieldConfiguration)
    {
        var path = $"fields[{index}]";

        if (string.IsNullOrWhiteSpace(field.Id))
        {
            errors.Add(Error($"{path}.id", "field.id_required", "Field id is required."));
        }
        else if (!fieldIds.Add(field.Id))
        {
            errors.Add(Error($"{path}.id", "field.duplicate_id", $"Field id '{field.Id}' is duplicated."));
        }

        if (string.IsNullOrWhiteSpace(field.Label))
        {
            errors.Add(Error($"{path}.label", "field.label_required", "Field label is required."));
        }

        if (string.IsNullOrWhiteSpace(field.Type) || !FormFieldTypes.Supported.Contains(field.Type))
        {
            errors.Add(Error($"{path}.type", "field.type_unknown", "Field type is not supported in V1."));
        }

        if (requireFieldConfiguration && FormFieldTypes.IsChoice(field.Type))
        {
            ValidateOptions(field, path, errors);
        }

        if (requireFieldConfiguration && string.Equals(field.Type, FormFieldTypes.RecordLookup, StringComparison.Ordinal))
        {
            ValidateLookup(field, path, errors);
        }

        if (requireFieldConfiguration && string.Equals(field.Type, FormFieldTypes.SubTable, StringComparison.Ordinal))
        {
            ValidateSubTable(field, path, errors);
        }
    }

    private static void ValidateOptions(
        FormFieldDefinition field,
        string path,
        List<FormValidationError> errors)
    {
        var options = field.Options ?? Array.Empty<FormFieldOptionDefinition>();
        var optionValues = new HashSet<string>(StringComparer.Ordinal);

        if (options.Count == 0)
        {
            errors.Add(Error($"{path}.options", "field.options_required", $"'{field.Label}' requires at least one option."));
        }

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var optionPath = $"{path}.options[{index}]";

            if (string.IsNullOrWhiteSpace(option.Id))
            {
                errors.Add(Error($"{optionPath}.id", "field.option_id_required", "Option id is required."));
            }

            if (string.IsNullOrWhiteSpace(option.Label))
            {
                errors.Add(Error($"{optionPath}.label", "field.option_label_required", "Option label is required."));
            }

            if (string.IsNullOrWhiteSpace(option.Value))
            {
                errors.Add(Error($"{optionPath}.value", "field.option_value_required", "Option value is required."));
            }
            else if (!optionValues.Add(option.Value))
            {
                errors.Add(Error($"{optionPath}.value", "field.option_value_duplicate", $"Option value '{option.Value}' is duplicated."));
            }
        }
    }

    private static void ValidateLookup(
        FormFieldDefinition field,
        string path,
        List<FormValidationError> errors)
    {
        if (field.Lookup is null)
        {
            errors.Add(Error($"{path}.lookup", "field.lookup_required", $"'{field.Label}' requires lookup configuration."));
            return;
        }

        if (!string.Equals(field.Lookup.SourceType, "form_records", StringComparison.Ordinal))
        {
            errors.Add(Error($"{path}.lookup.sourceType", "field.lookup_source_type", $"'{field.Label}' lookup source is not supported."));
        }

        if (!Guid.TryParse(field.Lookup.SourceFormId, out _))
        {
            errors.Add(Error($"{path}.lookup.sourceFormId", "field.lookup_source_form_required", $"'{field.Label}' requires a source form."));
        }

        if (field.Lookup.LabelFieldIds is null || field.Lookup.LabelFieldIds.Count == 0 || field.Lookup.LabelFieldIds.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(Error($"{path}.lookup.labelFieldIds", "field.lookup_label_fields_required", $"'{field.Label}' requires at least one label field."));
        }

        if (field.Lookup.SearchFieldIds is null || field.Lookup.SearchFieldIds.Count == 0 || field.Lookup.SearchFieldIds.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(Error($"{path}.lookup.searchFieldIds", "field.lookup_search_fields_required", $"'{field.Label}' requires at least one search field."));
        }

        var filters = field.Lookup.Filters ?? Array.Empty<FormFieldLookupFilterDefinition>();
        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
            if (string.IsNullOrWhiteSpace(filter.SourceFieldId) || string.IsNullOrWhiteSpace(filter.ValueFromFieldId))
            {
                errors.Add(Error($"{path}.lookup.filters[{index}]", "field.lookup_filter_required", $"'{field.Label}' lookup filters require source and parent fields."));
            }
        }
    }

    private static void ValidateSubTable(
        FormFieldDefinition field,
        string path,
        List<FormValidationError> errors)
    {
        if (field.SubTable is null)
        {
            errors.Add(Error($"{path}.subTable", "field.sub_table_required", $"'{field.Label}' requires sub-table configuration."));
            return;
        }

        if (!string.Equals(field.SubTable.SourceType, "child_form_records", StringComparison.Ordinal))
        {
            errors.Add(Error($"{path}.subTable.sourceType", "field.sub_table_source_type", $"'{field.Label}' sub-table source is not supported."));
        }

        if (!Guid.TryParse(field.SubTable.ChildFormId, out _))
        {
            errors.Add(Error($"{path}.subTable.childFormId", "field.sub_table_child_form_required", $"'{field.Label}' requires a child form."));
        }

        if (string.IsNullOrWhiteSpace(field.SubTable.ParentLookupFieldId))
        {
            errors.Add(Error($"{path}.subTable.parentLookupFieldId", "field.sub_table_parent_lookup_required", $"'{field.Label}' requires a parent lookup field."));
        }

        if (field.SubTable.DisplayColumnFieldIds is null || field.SubTable.DisplayColumnFieldIds.Count == 0 || field.SubTable.DisplayColumnFieldIds.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(Error($"{path}.subTable.displayColumnFieldIds", "field.sub_table_display_fields_required", $"'{field.Label}' requires at least one display column."));
        }

        if (field.SubTable.MinRows is < 0)
        {
            errors.Add(Error($"{path}.subTable.minRows", "field.sub_table_min_rows", $"'{field.Label}' minimum rows must be 0 or greater."));
        }

        if (field.SubTable.MaxRows is < 1)
        {
            errors.Add(Error($"{path}.subTable.maxRows", "field.sub_table_max_rows", $"'{field.Label}' maximum rows must be 1 or greater."));
        }

        if (field.SubTable.MinRows.HasValue && field.SubTable.MaxRows.HasValue && field.SubTable.MinRows.Value > field.SubTable.MaxRows.Value)
        {
            errors.Add(Error($"{path}.subTable.maxRows", "field.sub_table_row_range", $"'{field.Label}' minimum rows cannot exceed maximum rows."));
        }
    }

    private static void ValidateLayout(
        FormLayoutDefinition? layout,
        HashSet<string> fieldIds,
        List<FormValidationError> errors,
        bool requireRows)
    {
        var referencedFields = new HashSet<string>(StringComparer.Ordinal);

        if (layout?.Pages is null || layout.Pages.Count == 0)
        {
            errors.Add(Error("layout.pages", "layout.pages_required", "At least one layout page is required."));
            return;
        }

        for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            var page = layout.Pages[pageIndex];
            var pagePath = $"layout.pages[{pageIndex}]";

            if (string.IsNullOrWhiteSpace(page.Id))
            {
                errors.Add(Error($"{pagePath}.id", "layout.page_id_required", "Page id is required."));
            }

            if (page.Sections is null || page.Sections.Count == 0)
            {
                errors.Add(Error($"{pagePath}.sections", "layout.sections_required", "Each page requires at least one section."));
                continue;
            }

            for (var sectionIndex = 0; sectionIndex < page.Sections.Count; sectionIndex++)
            {
                ValidateSection(page.Sections[sectionIndex], $"{pagePath}.sections[{sectionIndex}]", fieldIds, referencedFields, errors, requireRows);
            }
        }

        foreach (var fieldId in fieldIds)
        {
            if (!referencedFields.Contains(fieldId))
            {
                errors.Add(Error("layout", "layout.field_missing", $"Field '{fieldId}' is not placed in the layout."));
            }
        }
    }

    private static void ValidateSection(
        FormLayoutSectionDefinition section,
        string sectionPath,
        HashSet<string> fieldIds,
        HashSet<string> referencedFields,
        List<FormValidationError> errors,
        bool requireRows)
    {
        if (string.IsNullOrWhiteSpace(section.Id))
        {
            errors.Add(Error($"{sectionPath}.id", "layout.section_id_required", "Section id is required."));
        }

        if (section.Rows is null)
        {
            errors.Add(Error($"{sectionPath}.rows", "layout.rows_required", "Section rows must be an array."));
            return;
        }

        if (requireRows && section.Rows.Count == 0)
        {
            errors.Add(Error($"{sectionPath}.rows", "layout.rows_required", "Each section requires at least one row."));
            return;
        }

        for (var rowIndex = 0; rowIndex < section.Rows.Count; rowIndex++)
        {
            ValidateRow(section.Rows[rowIndex], $"{sectionPath}.rows[{rowIndex}]", fieldIds, referencedFields, errors);
        }
    }

    private static void ValidateRow(
        FormLayoutRowDefinition row,
        string rowPath,
        HashSet<string> fieldIds,
        HashSet<string> referencedFields,
        List<FormValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(row.Id))
        {
            errors.Add(Error($"{rowPath}.id", "layout.row_id_required", "Row id is required."));
        }

        if (row.Columns is null || row.Columns.Count == 0)
        {
            errors.Add(Error($"{rowPath}.columns", "layout.columns_required", "Each row requires at least one column."));
            return;
        }

        for (var columnIndex = 0; columnIndex < row.Columns.Count; columnIndex++)
        {
            var column = row.Columns[columnIndex];
            var columnPath = $"{rowPath}.columns[{columnIndex}]";

            if (string.IsNullOrWhiteSpace(column.Id))
            {
                errors.Add(Error($"{columnPath}.id", "layout.column_id_required", "Column id is required."));
            }

            ValidateSpan(column.Span, columnPath, errors);
            ValidateLayoutFields(column.Fields, columnPath, fieldIds, referencedFields, errors);
        }
    }

    private static void ValidateSpan(
        ResponsiveSpanDefinition? span,
        string path,
        List<FormValidationError> errors)
    {
        if (span is null)
        {
            errors.Add(Error($"{path}.span", "layout.span_required", "Column span is required."));
            return;
        }

        ValidateSpanValue(span.Mobile, $"{path}.span.mobile", "mobile", errors);
        ValidateSpanValue(span.Tablet, $"{path}.span.tablet", "tablet", errors);
        ValidateSpanValue(span.Desktop, $"{path}.span.desktop", "desktop", errors);
    }

    private static void ValidateSpanValue(
        int value,
        string path,
        string breakpoint,
        List<FormValidationError> errors)
    {
        if (value < 1 || value > 12)
        {
            errors.Add(Error(path, "layout.span_invalid", $"{breakpoint} span must be an integer from 1 to 12."));
        }
    }

    private static void ValidateLayoutFields(
        IReadOnlyList<string>? fields,
        string path,
        HashSet<string> fieldIds,
        HashSet<string> referencedFields,
        List<FormValidationError> errors)
    {
        if (fields is null)
        {
            errors.Add(Error($"{path}.fields", "layout.fields_required", "Column fields must be an array."));
            return;
        }

        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            var fieldId = fields[fieldIndex];
            var fieldPath = $"{path}.fields[{fieldIndex}]";

            if (string.IsNullOrWhiteSpace(fieldId))
            {
                errors.Add(Error(fieldPath, "layout.field_id_required", "Layout field id is required."));
                continue;
            }

            if (!fieldIds.Contains(fieldId))
            {
                errors.Add(Error(fieldPath, "layout.field_unknown", $"Layout references unknown field '{fieldId}'."));
                continue;
            }

            if (!referencedFields.Add(fieldId))
            {
                errors.Add(Error(fieldPath, "layout.field_duplicate", $"Field '{fieldId}' is placed more than once."));
            }
        }
    }

    private static void ValidateRecordFieldValue(
        FormFieldDefinition field,
        object? value,
        List<FormValidationError> errors)
    {
        var path = $"values.{field.Id}";

        switch (field.Type)
        {
            case FormFieldTypes.Text:
            case FormFieldTypes.Textarea:
            case FormFieldTypes.Phone:
            case FormFieldTypes.FileUpload:
                if (!IsStringValue(value))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be text."));
                }

                return;
            case FormFieldTypes.Email:
                if (!IsStringValue(value))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be an email string."));
                    return;
                }

                if (!EmailPattern.IsMatch(AsString(value) ?? string.Empty))
                {
                    errors.Add(Error(path, "record.email", $"'{field.Label}' must be a valid email address."));
                }

                return;
            case FormFieldTypes.Number:
            case FormFieldTypes.Currency:
                if (!IsNumberValue(value))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be a finite number."));
                }

                return;
            case FormFieldTypes.Percent:
                if (!TryGetDecimal(value, out var percentValue))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be a finite number."));
                    return;
                }

                if (percentValue < 0 || percentValue > 100)
                {
                    errors.Add(Error(path, "record.percent", $"'{field.Label}' must be between 0 and 100."));
                }

                return;
            case FormFieldTypes.Rating:
                if (!TryGetDecimal(value, out var ratingValue))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be a whole number."));
                    return;
                }

                if (ratingValue != decimal.Truncate(ratingValue))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be a whole number."));
                    return;
                }

                if (ratingValue < 1 || ratingValue > 5)
                {
                    errors.Add(Error(path, "record.rating", $"'{field.Label}' must be a rating from 1 to 5."));
                }

                return;
            case FormFieldTypes.Date:
                if (!IsStringValue(value) || !DatePattern.IsMatch(AsString(value) ?? string.Empty))
                {
                    errors.Add(Error(path, "record.date", $"'{field.Label}' must use YYYY-MM-DD format."));
                }

                return;
            case FormFieldTypes.Time:
                if (!IsStringValue(value) || !TimePattern.IsMatch(AsString(value) ?? string.Empty))
                {
                    errors.Add(Error(path, "record.time", $"'{field.Label}' must use HH:mm format."));
                }

                return;
            case FormFieldTypes.Datetime:
                if (!IsStringValue(value) || !DatetimePattern.IsMatch(AsString(value) ?? string.Empty))
                {
                    errors.Add(Error(path, "record.datetime", $"'{field.Label}' must use date and time format."));
                }

                return;
            case FormFieldTypes.Url:
                if (!IsStringValue(value) || !IsHttpUrl(AsString(value) ?? string.Empty))
                {
                    errors.Add(Error(path, "record.url", $"'{field.Label}' must be a valid URL."));
                }

                return;
            case FormFieldTypes.Checkbox:
                if (!IsBooleanValue(value))
                {
                    errors.Add(Error(path, "record.type", $"'{field.Label}' must be true or false."));
                }

                return;
            case FormFieldTypes.RecordLookup:
                if (!IsStringValue(value) || !Guid.TryParse(AsString(value), out _))
                {
                    errors.Add(Error(path, "record.lookup_type", $"'{field.Label}' must be a selected record id."));
                }

                return;
            case FormFieldTypes.UserPicker:
                if (!IsStringValue(value) || !Guid.TryParse(AsString(value), out _))
                {
                    errors.Add(Error(path, "record.user_picker_type", $"'{field.Label}' must be a selected user id."));
                }

                return;
            case FormFieldTypes.DepartmentPicker:
                if (!IsStringValue(value) || !Guid.TryParse(AsString(value), out _))
                {
                    errors.Add(Error(path, "record.department_picker_type", $"'{field.Label}' must be a selected department id."));
                }

                return;
            case FormFieldTypes.SubTable:
                errors.Add(Error(path, "record.sub_table_readonly", $"'{field.Label}' is stored through related child records."));
                return;
        }

        if (FormFieldTypes.IsChoice(field.Type))
        {
            var optionValue = AsString(value);

            if (optionValue is null)
            {
                errors.Add(Error(path, "record.type", $"'{field.Label}' must be an option value."));
                return;
            }

            var allowedValues = field.Options?
                .Select(option => option.Value)
                .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);

            if (!allowedValues.Contains(optionValue))
            {
                errors.Add(Error(path, "record.option_unknown", $"'{field.Label}' has an unknown option value."));
            }
        }
    }

    private static bool IsEmptyValue(object? value)
    {
        return value is null
            || value is string { Length: 0 }
            || value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }
            || value is JsonElement { ValueKind: JsonValueKind.String } element && string.IsNullOrEmpty(element.GetString());
    }

    private static bool IsStringValue(object? value)
    {
        return value is string || value is JsonElement { ValueKind: JsonValueKind.String };
    }

    private static bool IsBooleanValue(object? value)
    {
        return value is bool || value is JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False };
    }

    private static bool IsNumberValue(object? value)
    {
        return TryGetDecimal(value, out _);
    }

    private static bool TryGetDecimal(object? value, out decimal number)
    {
        switch (value)
        {
            case byte byteValue:
                number = byteValue;
                return true;
            case sbyte signedByteValue:
                number = signedByteValue;
                return true;
            case short shortValue:
                number = shortValue;
                return true;
            case ushort unsignedShortValue:
                number = unsignedShortValue;
                return true;
            case int intValue:
                number = intValue;
                return true;
            case uint unsignedIntValue:
                number = unsignedIntValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case ulong unsignedLongValue:
                number = unsignedLongValue;
                return true;
            case float floatValue when !float.IsNaN(floatValue) && !float.IsInfinity(floatValue):
                number = (decimal)floatValue;
                return true;
            case double doubleValue when !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue):
                number = (decimal)doubleValue;
                return true;
            case decimal decimalValue:
                number = decimalValue;
                return true;
            case JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetDecimal(out var decimalValue):
                number = decimalValue;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static string? AsString(object? value)
    {
        return value switch
        {
            string stringValue => stringValue,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };
    }

    private static FormValidationError Error(string path, string code, string message)
    {
        return new FormValidationError(path, code, message);
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateEmailPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateDatePattern();

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateTimePattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateDatetimePattern();
}
