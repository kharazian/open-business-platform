using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class RecordImportJobValidator
{
    public static RecordImportValidationResult Validate(
        CreateRecordImportJobRequest request,
        FormSchemaDefinition schema,
        RecordImportCsvDocument csv)
    {
        var errors = new List<FormValidationError>();
        var headers = csv.Headers.ToHashSet(StringComparer.Ordinal);
        var fieldIds = schema.Fields.Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
        var mappedTargets = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.IntegrationKey))
        {
            errors.Add(new FormValidationError("integrationKey", "record_import.integration_key_required", "Integration key is required."));
        }

        if (csv.Headers.Count == 0)
        {
            errors.Add(new FormValidationError("csvContent", "record_import.csv_headers_required", "CSV headers are required."));
        }

        if (request.Mapping.FieldMappings.Count == 0)
        {
            errors.Add(new FormValidationError("mapping.fieldMappings", "record_import.mapping_required", "At least one field mapping is required."));
        }

        foreach (var mapping in request.Mapping.FieldMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.CsvHeader) || !headers.Contains(mapping.CsvHeader))
            {
                errors.Add(new FormValidationError("mapping.csvHeader", "record_import.header_missing", "Mapped CSV header does not exist."));
            }

            if (string.IsNullOrWhiteSpace(mapping.TargetFieldId) || !fieldIds.Contains(mapping.TargetFieldId))
            {
                errors.Add(new FormValidationError("mapping.targetFieldId", "record_import.target_field_unknown", "Mapped target field does not exist."));
            }
            else if (!mappedTargets.Add(mapping.TargetFieldId))
            {
                errors.Add(new FormValidationError("mapping.targetFieldId", "record_import.target_field_duplicate", "Target fields can only be mapped once."));
            }
        }

        return new RecordImportValidationResult(errors);
    }
}
