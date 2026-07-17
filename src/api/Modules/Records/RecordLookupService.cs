using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordLookupService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public RecordLookupService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PagedResultDto<RecordLookupOptionDto>> ListOptionsAsync(
        ClaimsPrincipal principal,
        Guid formId,
        string fieldId,
        RecordLookupOptionsRequest request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = Normalize(request.Search);
        var dependencyValues = request.DependencyValues is null
            ? new Dictionary<string, string?>(StringComparer.Ordinal)
            : new Dictionary<string, string?>(request.DependencyValues, StringComparer.Ordinal);

        var parentForm = await dbContext.Forms
            .AsNoTracking()
            .Include(form => form.CurrentVersion)
            .FirstOrDefaultAsync(form => form.Id == formId && !form.IsDeleted, cancellationToken);

        if (parentForm?.CurrentVersion is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var parentSchema = DeserializeSchema(parentForm.CurrentVersion.SchemaJson);
        if (parentSchema is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Form schema is invalid.");
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, formId, cancellationToken);
        if (fieldAccess.HiddenFieldIds.Contains(fieldId))
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Lookup field was not found.");
        }

        var lookupField = parentSchema.Fields.FirstOrDefault(field =>
            string.Equals(field.Id, fieldId, StringComparison.Ordinal)
            && string.Equals(field.Type, FormFieldTypes.RecordLookup, StringComparison.Ordinal));

        if (lookupField?.Lookup is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Lookup field was not found.");
        }

        if (!Guid.TryParse(lookupField.Lookup.SourceFormId, out var sourceFormId))
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Lookup source form is invalid.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, sourceFormId, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Lookup source access was denied.");
        }

        var sourceFormExists = await dbContext.Forms
            .AsNoTracking()
            .AnyAsync(form => form.Id == sourceFormId && !form.IsDeleted, cancellationToken);

        if (!sourceFormExists)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Lookup source form was not found.");
        }

        var sourceFieldAccess = await permissionService.GetFieldAccessAsync(principal, sourceFormId, cancellationToken);
        var visibleLabelFieldIds = lookupField.Lookup.LabelFieldIds
            .Where(fieldId => !sourceFieldAccess.HiddenFieldIds.Contains(fieldId))
            .ToArray();
        var visibleSearchFieldIds = lookupField.Lookup.SearchFieldIds
            .Where(fieldId => !sourceFieldAccess.HiddenFieldIds.Contains(fieldId))
            .ToArray();

        var query = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == sourceFormId && !record.IsDeleted),
            sourceFormId,
            PlatformPermissions.Form.View,
            cancellationToken);

        var records = await query
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .ToArrayAsync(cancellationToken);

        var candidates = records
            .Select(record => new
            {
                Record = record,
                Values = DeserializeValues(record.ValuesJson)
            })
            .Where(item => MatchesLookupFilters(item.Values, lookupField.Lookup.Filters, dependencyValues))
            .Where(item => search is null || MatchesLookupSearch(item.Values, visibleSearchFieldIds, search))
            .ToArray();

        var items = candidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new RecordLookupOptionDto(
                item.Record.Id,
                ComposeLookupLabel(item.Values, visibleLabelFieldIds),
                ComposeLookupDescription(item.Values, visibleSearchFieldIds, visibleLabelFieldIds)))
            .ToArray();

        return new PagedResultDto<RecordLookupOptionDto>(candidates.LongLength, items);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ResolveLookupDisplayValuesAsync(
        ClaimsPrincipal principal,
        FormSchemaDefinition schema,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> valueSets,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var displayValues = valueSets
            .Select(_ => new Dictionary<string, string>(StringComparer.Ordinal))
            .ToArray();

        if (valueSets.Count == 0)
        {
            return displayValues;
        }

        var addressFields = schema.Fields.Where(field => string.Equals(field.Type, FormFieldTypes.Address, StringComparison.Ordinal)).ToArray();
        for (var index = 0; index < valueSets.Count; index++)
        {
            foreach (var field in addressFields)
            {
                if (valueSets[index].TryGetValue(field.Id, out var value) && FormAddressValueFormatter.TryFormat(value, out var formatted) && formatted.Length > 0)
                    displayValues[index][field.Id] = formatted;
            }
        }

        var lookupFields = schema.Fields
            .Where(field => string.Equals(field.Type, FormFieldTypes.RecordLookup, StringComparison.Ordinal))
            .Where(field => field.Lookup is not null)
            .ToArray();

        foreach (var field in lookupFields)
        {
            var lookup = field.Lookup!;
            if (!string.Equals(lookup.SourceType, "form_records", StringComparison.Ordinal)
                || !Guid.TryParse(lookup.SourceFormId, out var sourceFormId)
                || !await permissionService.CanAccessFormAsync(principal, sourceFormId, PlatformPermissions.Form.View, cancellationToken))
            {
                continue;
            }

            var selectedValues = new List<SelectedLookupValue>();
            for (var index = 0; index < valueSets.Count; index++)
            {
                if (valueSets[index].TryGetValue(field.Id, out var value) && TryGetLookupRecordId(value, out var recordId))
                {
                    selectedValues.Add(new SelectedLookupValue(index, recordId));
                }
            }

            if (selectedValues.Count == 0)
            {
                continue;
            }

            var sourceFieldAccess = await permissionService.GetFieldAccessAsync(principal, sourceFormId, cancellationToken);
            var visibleLabelFieldIds = lookup.LabelFieldIds
                .Where(fieldId => !sourceFieldAccess.HiddenFieldIds.Contains(fieldId))
                .ToArray();
            var selectedRecordIds = selectedValues
                .Select(item => item.RecordId)
                .Distinct()
                .ToArray();

            var sourceQuery = await permissionService.ApplyRecordAccessAsync(
                principal,
                dbContext.Records.AsNoTracking().Where(record =>
                    record.FormId == sourceFormId
                    && !record.IsDeleted
                    && selectedRecordIds.Contains(record.Id)),
                sourceFormId,
                PlatformPermissions.Form.View,
                cancellationToken);

            var labelsByRecordId = (await sourceQuery.ToArrayAsync(cancellationToken))
                .ToDictionary(
                    record => record.Id,
                    record => ComposeLookupLabel(DeserializeValues(record.ValuesJson), visibleLabelFieldIds));

            foreach (var selectedValue in selectedValues)
            {
                if (labelsByRecordId.TryGetValue(selectedValue.RecordId, out var label))
                {
                    displayValues[selectedValue.Index][field.Id] = label;
                }
            }
        }

        return displayValues;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>>> ResolveLookupDisplayValuesByRecordIdAsync(
        ClaimsPrincipal principal,
        FormSchemaDefinition schema,
        IReadOnlyCollection<FormRecord> records,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var recordArray = records.ToArray();
        var valueSets = recordArray
            .Select(record => DeserializeValues(record.ValuesJson))
            .ToArray();
        var resolved = await ResolveLookupDisplayValuesAsync(principal, schema, valueSets, permissionService, cancellationToken);

        return recordArray
            .Select((record, index) => new { record.Id, Values = resolved[index] })
            .Where(item => item.Values.Count > 0)
            .ToDictionary(
                item => item.Id,
                item => (IReadOnlyDictionary<string, string>)item.Values,
                EqualityComparer<Guid>.Default);
    }

    public async Task<IReadOnlyList<FormValidationError>> ValidateLookupValuesAsync(
        ClaimsPrincipal principal,
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, object?> values,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var errors = new List<FormValidationError>();

        foreach (var field in schema.Fields.Where(field => string.Equals(field.Type, FormFieldTypes.RecordLookup, StringComparison.Ordinal)))
        {
            if (!values.TryGetValue(field.Id, out var value) || IsEmptyLookupValue(value))
            {
                continue;
            }

            if (!TryGetLookupRecordId(value, out var selectedRecordId))
            {
                errors.Add(LookupValueError(field, "record.lookup_type", $"'{field.Label}' must be a selected record id."));
                continue;
            }

            if (field.Lookup is null
                || !string.Equals(field.Lookup.SourceType, "form_records", StringComparison.Ordinal)
                || !Guid.TryParse(field.Lookup.SourceFormId, out var sourceFormId))
            {
                errors.Add(LookupValueError(field, "record.lookup_source_invalid", $"'{field.Label}' lookup source is invalid."));
                continue;
            }

            if (!await permissionService.CanAccessFormAsync(principal, sourceFormId, PlatformPermissions.Form.View, cancellationToken))
            {
                errors.Add(LookupValueError(field, "record.lookup_record_unknown", $"'{field.Label}' selected record was not found or is not accessible."));
                continue;
            }

            var selectedRecord = await dbContext.Records
                .AsNoTracking()
                .FirstOrDefaultAsync(record =>
                    record.Id == selectedRecordId
                    && record.FormId == sourceFormId
                    && !record.IsDeleted,
                    cancellationToken);

            if (selectedRecord is null
                || !await permissionService.CanAccessRecordAsync(principal, selectedRecord, PlatformPermissions.Form.View, cancellationToken))
            {
                errors.Add(LookupValueError(field, "record.lookup_record_unknown", $"'{field.Label}' selected record was not found or is not accessible."));
            }
        }

        return errors;
    }

    public static bool IsRecordLookupValue(object? value)
    {
        return TryGetLookupRecordId(value, out _);
    }

    public static string ComposeLookupLabel(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<string> labelFieldIds)
    {
        var parts = labelFieldIds
            .Select(fieldId => values.TryGetValue(fieldId, out var value) ? ToDisplayString(value) : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        return parts.Length == 0 ? "(Untitled record)" : string.Join(" - ", parts);
    }

    public static bool MatchesLookupSearch(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<string> searchFieldIds,
        string search)
    {
        var normalizedSearch = Normalize(search);
        if (normalizedSearch is null)
        {
            return true;
        }

        return searchFieldIds.Any(fieldId =>
            values.TryGetValue(fieldId, out var value)
            && ToDisplayString(value)?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true);
    }

    public static bool MatchesLookupFilters(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<FormFieldLookupFilterDefinition>? filters,
        IReadOnlyDictionary<string, string?> dependencyValues)
    {
        if (filters is null || filters.Count == 0)
        {
            return true;
        }

        return filters.All(filter =>
        {
            if (string.IsNullOrWhiteSpace(filter.SourceFieldId)
                || string.IsNullOrWhiteSpace(filter.ValueFromFieldId)
                || !dependencyValues.TryGetValue(filter.ValueFromFieldId, out var dependencyValue)
                || string.IsNullOrWhiteSpace(dependencyValue))
            {
                return false;
            }

            return values.TryGetValue(filter.SourceFieldId, out var sourceValue)
                && string.Equals(
                    Normalize(ToDisplayString(sourceValue)),
                    Normalize(dependencyValue),
                    StringComparison.OrdinalIgnoreCase);
        });
    }

    private static FormValidationError LookupValueError(FormFieldDefinition field, string code, string message)
    {
        return new FormValidationError($"values.{field.Id}", code, message);
    }

    private static bool IsEmptyLookupValue(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
        {
            return true;
        }

        var text = AsLookupString(value);
        return text is not null && string.IsNullOrWhiteSpace(text);
    }

    private static bool TryGetLookupRecordId(object? value, out Guid recordId)
    {
        recordId = Guid.Empty;
        var text = AsLookupString(value);
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out recordId);
    }

    private static string? AsLookupString(object? value)
    {
        return value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };
    }

    private static string? ComposeLookupDescription(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<string> searchFieldIds,
        IReadOnlyCollection<string> labelFieldIds)
    {
        var labelIds = labelFieldIds.ToHashSet(StringComparer.Ordinal);
        var parts = searchFieldIds
            .Where(fieldId => !labelIds.Contains(fieldId))
            .Select(fieldId => values.TryGetValue(fieldId, out var value) ? ToDisplayString(value) : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" - ", parts);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static string? ToDisplayString(object? value)
    {
        if (FormAddressValueFormatter.TryFormat(value, out var address)) return address;
        return value switch
        {
            null => null,
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonElement element => element.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record SelectedLookupValue(int Index, Guid RecordId);
}
