using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public sealed record RelatedReportField(
    string Key,
    string LookupFieldId,
    Guid TargetFormId,
    string TargetFieldId,
    ReportableFieldMetadata Metadata,
    FormSchemaDefinition TargetSchema);

public sealed record ReportFieldCatalog(
    IReadOnlyDictionary<string, ReportableFieldMetadata> Fields,
    IReadOnlyDictionary<string, RelatedReportField> RelatedFields);

public sealed class ReportRelationshipFieldService(
    OpenBusinessPlatformDbContext dbContext,
    RecordLookupService recordLookup)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportFieldCatalog> BuildStructuralCatalogAsync(
        Guid rootFormId,
        FormSchemaDefinition rootSchema,
        CancellationToken ct)
    {
        var fields = FormReportableFieldMetadata.GetReportableFieldsById(rootSchema)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var related = new Dictionary<string, RelatedReportField>(StringComparer.Ordinal);
        var lookups = rootSchema.Fields
            .Where(field => field.Type == FormFieldTypes.RecordLookup && field.Lookup is not null)
            .Select(field => new { Field = field, ValidTarget = Guid.TryParse(field.Lookup!.SourceFormId, out var targetFormId), TargetFormId = targetFormId })
            .Where(item => item.ValidTarget && item.TargetFormId != rootFormId)
            .ToArray();

        var targetFormIds = lookups.Select(item => item.TargetFormId).Distinct().ToArray();
        var targetForms = await dbContext.Forms.AsNoTracking()
            .Include(form => form.CurrentVersion)
            .Where(form => targetFormIds.Contains(form.Id) && !form.IsDeleted)
            .ToDictionaryAsync(form => form.Id, ct);

        foreach (var lookup in lookups)
        {
            if (!targetForms.TryGetValue(lookup.TargetFormId, out var targetForm)) continue;
            var targetSchema = DeserializeSchema(targetForm.CurrentVersion?.SchemaJson) ?? DeserializeSchema(targetForm.DraftSchemaJson);
            if (targetSchema is null) continue;

            foreach (var targetField in FormReportableFieldMetadata.GetReportableFields(targetSchema))
            {
                var key = $"{lookup.Field.Id}.{targetField.Id}";
                var metadata = targetField with
                {
                    Id = key,
                    Label = $"{lookup.Field.Label} › {targetField.Label}",
                    Source = ReportableFieldSources.Relationship
                };
                fields[key] = metadata;
                related[key] = new RelatedReportField(key, lookup.Field.Id, lookup.TargetFormId, targetField.Id, metadata, targetSchema);
            }
        }

        return new ReportFieldCatalog(fields, related);
    }

    public async Task<ReportFieldCatalog> FilterPermittedAsync(
        ClaimsPrincipal principal,
        Guid rootFormId,
        FormSchemaDefinition rootSchema,
        ReportFieldCatalog structural,
        string action,
        PermissionService permissions,
        CancellationToken ct)
    {
        var rootFieldAccess = await permissions.GetFieldAccessAsync(principal, rootFormId, ct);
        var fields = structural.Fields
            .Where(pair => !pair.Key.Contains('.', StringComparison.Ordinal) && !rootFieldAccess.HiddenFieldIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var related = new Dictionary<string, RelatedReportField>(StringComparer.Ordinal);

        foreach (var targetGroup in structural.RelatedFields.Values.GroupBy(field => field.TargetFormId))
        {
            if (!await permissions.CanAccessFormAsync(principal, targetGroup.Key, action, ct)) continue;
            var targetFieldAccess = await permissions.GetFieldAccessAsync(principal, targetGroup.Key, ct);

            foreach (var field in targetGroup)
            {
                if (rootFieldAccess.HiddenFieldIds.Contains(field.LookupFieldId)
                    || targetFieldAccess.HiddenFieldIds.Contains(field.TargetFieldId)) continue;
                fields[field.Key] = field.Metadata;
                related[field.Key] = field;
            }
        }

        return new ReportFieldCatalog(fields, related);
    }

    public IReadOnlyList<ReportValidationError> ValidatePaths(
        Guid rootFormId,
        FormSchemaDefinition rootSchema,
        ListReportConfigDefinition config,
        ReportFieldCatalog catalog)
    {
        var errors = new List<ReportValidationError>();
        ValidatePathCollection(config.Columns.Select((item, index) => (item.FieldId, $"config.columns[{index}].fieldId")), rootFormId, rootSchema, catalog, errors);
        ValidatePathCollection(config.Filters.Select((item, index) => (item.FieldId, $"config.filters[{index}].fieldId")), rootFormId, rootSchema, catalog, errors);
        ValidatePathCollection(config.Sort.Select((item, index) => (item.FieldId, $"config.sort[{index}].fieldId")), rootFormId, rootSchema, catalog, errors);
        return errors;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>>> ResolveAsync(
        ClaimsPrincipal principal,
        IReadOnlyCollection<FormRecord> rootRecords,
        IReadOnlyCollection<string> requestedFieldIds,
        ReportFieldCatalog permittedCatalog,
        string action,
        PermissionService permissions,
        CancellationToken ct)
    {
        var requested = requestedFieldIds
            .Distinct(StringComparer.Ordinal)
            .Where(permittedCatalog.RelatedFields.ContainsKey)
            .Select(key => permittedCatalog.RelatedFields[key])
            .ToArray();
        if (requested.Length == 0 || rootRecords.Count == 0)
            return new Dictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>>();

        var rootValues = rootRecords.ToDictionary(record => record.Id, record => DeserializeValues(record.ValuesJson));
        var result = rootRecords.ToDictionary(
            record => record.Id,
            _ => new Dictionary<string, ResolvedReportFieldValue>(StringComparer.Ordinal));

        foreach (var targetGroup in requested.GroupBy(field => field.TargetFormId))
        {
            var lookupFieldIds = targetGroup.Select(field => field.LookupFieldId).Distinct(StringComparer.Ordinal).ToArray();
            var targetIds = rootValues.Values
                .SelectMany(values => lookupFieldIds.Select(fieldId => TryGuid(values, fieldId)))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToArray();
            if (targetIds.Length == 0) continue;

            var targetQuery = dbContext.Records.AsNoTracking()
                .Where(record => record.FormId == targetGroup.Key && targetIds.Contains(record.Id) && !record.IsDeleted);
            var scopedTargetQuery = await permissions.ApplyRecordAccessAsync(principal, targetQuery, targetGroup.Key, action, ct);
            var targetRecords = await scopedTargetQuery.ToArrayAsync(ct);
            var targetsById = targetRecords.ToDictionary(record => record.Id);
            var targetValues = targetRecords.ToDictionary(record => record.Id, record => DeserializeValues(record.ValuesJson));

            var lookupDisplaysBySchema = new Dictionary<FormSchemaDefinition, IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>>>();
            foreach (var schemaGroup in targetGroup.GroupBy(field => field.TargetSchema))
            {
                var lookupFieldIdsForSchema = schemaGroup.Where(field => field.Metadata.Type == FormFieldTypes.RecordLookup).Select(field => field.TargetFieldId).ToHashSet(StringComparer.Ordinal);
                if (lookupFieldIdsForSchema.Count == 0) continue;
                var displaySchema = schemaGroup.Key with { Fields = schemaGroup.Key.Fields.Where(field => lookupFieldIdsForSchema.Contains(field.Id)).ToArray() };
                lookupDisplaysBySchema[schemaGroup.Key] = await recordLookup.ResolveLookupDisplayValuesByRecordIdAsync(principal, displaySchema, targetRecords, permissions, ct);
            }

            foreach (var rootRecord in rootRecords)
            {
                foreach (var field in targetGroup)
                {
                    var targetId = TryGuid(rootValues[rootRecord.Id], field.LookupFieldId);
                    if (targetId is null || !targetsById.TryGetValue(targetId.Value, out var targetRecord)) continue;
                    var rawValue = GetFieldValue(targetRecord, targetValues[targetRecord.Id], field.TargetFieldId);
                    var displayValue = ToDisplayValue(rawValue);
                    if (field.Metadata.Type == FormFieldTypes.RecordLookup)
                    {
                        if (!lookupDisplaysBySchema.TryGetValue(field.TargetSchema, out var displays)
                            || !displays.TryGetValue(targetRecord.Id, out var recordDisplays)
                            || !recordDisplays.TryGetValue(field.TargetFieldId, out displayValue))
                        {
                            rawValue = null;
                            displayValue = string.Empty;
                        }
                    }
                    else if (field.Metadata.Type == FormFieldTypes.Address && FormAddressValueFormatter.TryFormat(rawValue, out var addressDisplay))
                    {
                        displayValue = addressDisplay;
                    }
                    result[rootRecord.Id][field.Key] = new ResolvedReportFieldValue(rawValue, displayValue);
                }
            }
        }

        return result.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<string, ResolvedReportFieldValue>)pair.Value);
    }

    private static void ValidatePathCollection(
        IEnumerable<(string FieldId, string Path)> items,
        Guid rootFormId,
        FormSchemaDefinition rootSchema,
        ReportFieldCatalog catalog,
        List<ReportValidationError> errors)
    {
        foreach (var (rawFieldId, path) in items)
        {
            var fieldId = rawFieldId.Trim();
            if (!fieldId.Contains('.', StringComparison.Ordinal)) continue;
            var segments = fieldId.Split('.');
            if (segments.Length != 2 || segments.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add(new(path, segments.Length > 2 ? "report.relationship.depth" : "report.relationship.path", "Related report fields must use one lookup hop."));
                continue;
            }
            var lookup = rootSchema.Fields.FirstOrDefault(field => field.Id == segments[0]);
            if (lookup is null || lookup.Type != FormFieldTypes.RecordLookup || lookup.Lookup is null)
            {
                errors.Add(new(path, "report.relationship.lookup", "Related report field must start with a record lookup field."));
                continue;
            }
            if (Guid.TryParse(lookup.Lookup.SourceFormId, out var targetFormId) && targetFormId == rootFormId)
            {
                errors.Add(new(path, "report.relationship.cycle", "Cyclic related report paths are not supported."));
                continue;
            }
            if (!catalog.RelatedFields.ContainsKey(fieldId))
                errors.Add(new(path, "report.relationship.unknown", "Related report field does not exist."));
        }
    }

    private static Guid? TryGuid(IReadOnlyDictionary<string, object?> values, string fieldId)
    {
        if (!values.TryGetValue(fieldId, out var value)) return null;
        var text = value switch { string item => item, JsonElement { ValueKind: JsonValueKind.String } item => item.GetString(), _ => null };
        return Guid.TryParse(text, out var id) ? id : null;
    }

    private static object? GetFieldValue(FormRecord record, IReadOnlyDictionary<string, object?> values, string fieldId) => fieldId switch
    {
        ReportableSystemFields.Status => record.Status,
        ReportableSystemFields.CreatedAt => record.CreatedAt,
        ReportableSystemFields.CreatedById => record.CreatedById,
        ReportableSystemFields.UpdatedAt => record.UpdatedAt,
        ReportableSystemFields.UpdatedById => record.UpdatedById,
        ReportableSystemFields.OwnerId => record.OwnerId,
        ReportableSystemFields.DepartmentId => record.DepartmentId,
        _ => values.TryGetValue(fieldId, out var value) ? value : null
    };

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument values) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values.RootElement.GetRawText(), JsonOptions)?
            .ToDictionary(pair => pair.Key, pair => ConvertJsonValue(pair.Value), StringComparer.Ordinal)
        ?? new Dictionary<string, object?>(StringComparer.Ordinal);

    private static object? ConvertJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt64(out var number) => number,
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.Clone()
    };

    private static string ToDisplayValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dateTime => dateTime.ToString("u", CultureInfo.InvariantCulture),
        Guid guid => guid.ToString(),
        bool boolean => boolean ? "Yes" : "No",
        JsonElement json => json.GetRawText(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schema) =>
        schema?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
}
