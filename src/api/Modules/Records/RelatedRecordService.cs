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

public sealed class RelatedRecordService(
    OpenBusinessPlatformDbContext dbContext,
    RecordLookupService recordLookup)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResultDto<RelatedRecordPanelDto>> ListPanelsAsync(
        ClaimsPrincipal principal,
        Guid targetRecordId,
        int requestedPage,
        int requestedPageSize,
        PermissionService permissions,
        CancellationToken ct)
    {
        var target = await GetAuthorizedTargetAsync(principal, targetRecordId, permissions, ct);
        var definitions = await DiscoverDefinitionsAsync(target.FormId, ct);
        var permitted = new List<PermittedPanel>();

        foreach (var definition in definitions)
        {
            if (!await permissions.CanAccessFormAsync(principal, definition.SourceFormId, PlatformPermissions.Form.View, ct))
                continue;

            var fieldAccess = await permissions.GetFieldAccessAsync(principal, definition.SourceFormId, ct);
            if (fieldAccess.HiddenFieldIds.Contains(definition.SourceFieldId))
                continue;

            permitted.Add(new PermittedPanel(
                definition,
                BuildPreviewColumns(definition.PreviewSchema, definition.SourceFieldId, fieldAccess.HiddenFieldIds)));
        }

        var ordered = permitted
            .OrderBy(item => item.Definition.SourceFormName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Definition.SourceFieldLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Definition.SourceFormId)
            .ThenBy(item => item.Definition.SourceFieldId, StringComparer.Ordinal)
            .ToArray();
        var page = Math.Max(1, requestedPage);
        var pageSize = Math.Clamp(requestedPageSize, 1, 25);
        var pageDefinitions = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var items = new List<RelatedRecordPanelDto>(pageDefinitions.Length);

        foreach (var panel in pageDefinitions)
        {
            var records = await FindRelatedRecordsAsync(principal, target, panel.Definition, permissions, ct);
            items.Add(ToPanelDto(panel, records.LongLength));
        }

        return new PagedResultDto<RelatedRecordPanelDto>(ordered.LongLength, items);
    }

    public async Task<RelatedRecordRowsDto> ListRowsAsync(
        ClaimsPrincipal principal,
        Guid targetRecordId,
        Guid sourceFormId,
        string sourceFieldId,
        int requestedPage,
        int requestedPageSize,
        PermissionService permissions,
        CancellationToken ct)
    {
        var target = await GetAuthorizedTargetAsync(principal, targetRecordId, permissions, ct);
        var definition = (await DiscoverDefinitionsAsync(target.FormId, ct)).FirstOrDefault(item =>
            item.SourceFormId == sourceFormId
            && string.Equals(item.SourceFieldId, sourceFieldId, StringComparison.Ordinal));

        if (definition is null
            || !await permissions.CanAccessFormAsync(principal, sourceFormId, PlatformPermissions.Form.View, ct))
            throw PanelNotFound();

        var fieldAccess = await permissions.GetFieldAccessAsync(principal, sourceFormId, ct);
        if (fieldAccess.HiddenFieldIds.Contains(sourceFieldId))
            throw PanelNotFound();

        var columns = BuildPreviewColumns(definition.PreviewSchema, sourceFieldId, fieldAccess.HiddenFieldIds);
        var panel = new PermittedPanel(definition, columns);
        var records = await FindRelatedRecordsAsync(principal, target, definition, permissions, ct);
        var page = Math.Max(1, requestedPage);
        var pageSize = Math.Clamp(requestedPageSize, 1, 50);
        var pageRecords = records.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var projectedValues = pageRecords
            .Select(record => ProjectValues(DeserializeValues(record.ValuesJson), columns))
            .ToArray();
        var displaySchema = definition.PreviewSchema with
        {
            Fields = definition.PreviewSchema.Fields.Where(field => columns.Any(column => column.FieldId == field.Id)).ToArray()
        };
        var displayValues = await recordLookup.ResolveLookupDisplayValuesAsync(
            principal,
            displaySchema,
            projectedValues,
            permissions,
            ct);
        var attachmentNames = await ResolveAttachmentNamesAsync(pageRecords, columns, ct);
        var fieldsById = displaySchema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var rows = pageRecords.Select((record, index) => new RelatedRecordRowDto(
            record.Id,
            record.Status,
            record.CreatedAt,
            columns.ToDictionary(
                column => column.FieldId,
                column => fieldsById.TryGetValue(column.FieldId, out var field)
                    ? FormatCell(
                        field,
                        projectedValues[index].TryGetValue(column.FieldId, out var rawValue) ? rawValue : null,
                        field.Type == FormFieldTypes.FileUpload
                            ? attachmentNames.TryGetValue((record.Id, field.Id), out var attachmentName) ? attachmentName : null
                            : displayValues[index].TryGetValue(column.FieldId, out var displayValue) ? displayValue : null)
                    : string.Empty,
                StringComparer.Ordinal))).ToArray();

        return new RelatedRecordRowsDto(ToPanelDto(panel, records.LongLength), page, pageSize, rows);
    }

    public static IReadOnlyList<RelatedRecordColumnDto> BuildPreviewColumns(
        FormSchemaDefinition schema,
        string backlinkFieldId,
        IReadOnlySet<string> hiddenFieldIds)
    {
        var fieldsById = schema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var orderedIds = schema.Layout.Pages
            .SelectMany(page => page.Sections)
            .SelectMany(section => section.Rows)
            .SelectMany(row => row.Columns)
            .SelectMany(column => column.Fields)
            .Concat(schema.Fields.Select(field => field.Id))
            .Distinct(StringComparer.Ordinal);

        return orderedIds
            .Where(fieldsById.ContainsKey)
            .Select(fieldId => fieldsById[fieldId])
            .Where(field => !string.Equals(field.Id, backlinkFieldId, StringComparison.Ordinal))
            .Where(field => !hiddenFieldIds.Contains(field.Id))
            .Where(field => !string.Equals(field.Type, FormFieldTypes.SubTable, StringComparison.Ordinal))
            .Take(5)
            .Select(field => new RelatedRecordColumnDto(field.Id, field.Label, field.Type))
            .ToArray();
    }

    public static bool IsRelationshipMatch(
        FormSchemaDefinition schema,
        IReadOnlyDictionary<string, object?> values,
        string sourceFieldId,
        Guid targetFormId,
        Guid targetRecordId)
    {
        var field = schema.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, sourceFieldId, StringComparison.Ordinal)
            && string.Equals(candidate.Type, FormFieldTypes.RecordLookup, StringComparison.Ordinal));
        return field?.Lookup is not null
            && string.Equals(field.Lookup.SourceType, "form_records", StringComparison.Ordinal)
            && Guid.TryParse(field.Lookup.SourceFormId, out var configuredTargetFormId)
            && configuredTargetFormId == targetFormId
            && values.TryGetValue(sourceFieldId, out var value)
            && TryGuid(value, out var configuredTargetRecordId)
            && configuredTargetRecordId == targetRecordId;
    }

    public static string FormatCell(FormFieldDefinition field, object? value, string? resolvedDisplayValue)
    {
        if (!string.IsNullOrWhiteSpace(resolvedDisplayValue)) return resolvedDisplayValue;
        if (field.Type is FormFieldTypes.RecordLookup or FormFieldTypes.FileUpload) return string.Empty;
        if (FormAddressValueFormatter.TryFormat(value, out var address)) return address;
        return value switch
        {
            null => string.Empty,
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() ?? string.Empty,
            JsonElement { ValueKind: JsonValueKind.True } => "True",
            JsonElement { ValueKind: JsonValueKind.False } => "False",
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => string.Empty,
            JsonElement element => element.ToString(),
            bool boolean => boolean ? "True" : "False",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private async Task<FormRecord> GetAuthorizedTargetAsync(
        ClaimsPrincipal principal,
        Guid targetRecordId,
        PermissionService permissions,
        CancellationToken ct)
    {
        var target = await dbContext.Records.AsNoTracking()
            .FirstOrDefaultAsync(record => record.Id == targetRecordId && !record.IsDeleted, ct);
        if (target is null)
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Record was not found.");
        if (!await permissions.CanAccessRecordAsync(principal, target, PlatformPermissions.Form.View, ct))
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Record access was denied.");
        return target;
    }

    private async Task<RelatedPanelDefinition[]> DiscoverDefinitionsAsync(Guid targetFormId, CancellationToken ct)
    {
        var forms = await dbContext.Forms.AsNoTracking()
            .Include(form => form.CurrentVersion)
            .Where(form => !form.IsDeleted)
            .ToDictionaryAsync(form => form.Id, ct);
        var versions = await dbContext.FormVersions.AsNoTracking()
            .Where(version => forms.Keys.Contains(version.FormId))
            .OrderByDescending(version => version.VersionNumber)
            .ToArrayAsync(ct);
        var candidates = new List<VersionedDefinition>();

        foreach (var version in versions)
        {
            var schema = DeserializeSchema(version.SchemaJson);
            if (schema is null || !forms.TryGetValue(version.FormId, out var form)) continue;
            foreach (var field in schema.Fields.Where(field => IsLookupToForm(field, targetFormId)))
                candidates.Add(new VersionedDefinition(form, version.VersionNumber, schema, field));
        }

        return candidates
            .GroupBy(candidate => new { FormId = candidate.Form.Id, FieldId = candidate.Field.Id })
            .Select(group => group.OrderByDescending(candidate => candidate.VersionNumber).First())
            .Select(candidate =>
            {
                var currentSchema = DeserializeSchema(candidate.Form.CurrentVersion?.SchemaJson);
                var currentRelationshipField = currentSchema?.Fields.FirstOrDefault(field =>
                    string.Equals(field.Id, candidate.Field.Id, StringComparison.Ordinal)
                    && IsLookupToForm(field, targetFormId));
                return new RelatedPanelDefinition(
                    candidate.Form.Id,
                    candidate.Form.Name,
                    candidate.Field.Id,
                    currentRelationshipField?.Label ?? candidate.Field.Label,
                    currentSchema ?? candidate.Schema);
            })
            .ToArray();
    }

    private async Task<FormRecord[]> FindRelatedRecordsAsync(
        ClaimsPrincipal principal,
        FormRecord target,
        RelatedPanelDefinition definition,
        PermissionService permissions,
        CancellationToken ct)
    {
        var scoped = await permissions.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == definition.SourceFormId && !record.IsDeleted),
            definition.SourceFormId,
            PlatformPermissions.Form.View,
            ct);
        var canonical = await scoped.Include(record => record.FormVersion)
            .Where(record => dbContext.RecordRelationships.Any(edge =>
                edge.SourceRecordId == record.Id
                && edge.SourceFieldId == definition.SourceFieldId
                && edge.TargetFormId == target.FormId
                && edge.TargetRecordId == target.Id))
            .ToArrayAsync(ct);
        using var compatibilityProbe = JsonSerializer.SerializeToDocument(
            new Dictionary<string, string>(StringComparer.Ordinal) { [definition.SourceFieldId] = target.Id.ToString() },
            JsonOptions);
        var compatible = await scoped.Include(record => record.FormVersion)
            .Where(record => EF.Functions.JsonContains(record.ValuesJson, compatibilityProbe))
            .ToArrayAsync(ct);

        return canonical.Concat(compatible)
            .GroupBy(record => record.Id)
            .Select(group => group.First())
            .Where(record => record.FormVersion is not null)
            .Where(record =>
            {
                var schema = DeserializeSchema(record.FormVersion!.SchemaJson);
                return schema is not null && IsRelationshipMatch(
                    schema,
                    DeserializeValues(record.ValuesJson),
                    definition.SourceFieldId,
                    target.FormId,
                    target.Id);
            })
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<(Guid RecordId, string FieldId), string>> ResolveAttachmentNamesAsync(
        IReadOnlyCollection<FormRecord> records,
        IReadOnlyCollection<RelatedRecordColumnDto> columns,
        CancellationToken ct)
    {
        var recordIds = records.Select(record => record.Id).ToArray();
        var fieldIds = columns.Where(column => column.Type == FormFieldTypes.FileUpload).Select(column => column.FieldId).ToArray();
        if (recordIds.Length == 0 || fieldIds.Length == 0)
            return new Dictionary<(Guid RecordId, string FieldId), string>();

        var attachments = await dbContext.FileAttachments.AsNoTracking()
            .Where(attachment => attachment.RecordId.HasValue
                && recordIds.Contains(attachment.RecordId.Value)
                && fieldIds.Contains(attachment.FieldId)
                && attachment.Status == AttachmentStatuses.Attached)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .Select(attachment => new { RecordId = attachment.RecordId!.Value, attachment.FieldId, attachment.FileName })
            .ToArrayAsync(ct);
        return attachments
            .GroupBy(attachment => (attachment.RecordId, attachment.FieldId))
            .ToDictionary(group => group.Key, group => group.First().FileName);
    }

    private static RelatedRecordPanelDto ToPanelDto(PermittedPanel panel, long totalCount) => new(
        panel.Definition.SourceFormId,
        panel.Definition.SourceFormName,
        panel.Definition.SourceFieldId,
        panel.Definition.SourceFieldLabel,
        panel.Columns,
        totalCount);

    private static IReadOnlyDictionary<string, object?> ProjectValues(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<RelatedRecordColumnDto> columns) => columns
        .Where(column => values.ContainsKey(column.FieldId))
        .ToDictionary(column => column.FieldId, column => values[column.FieldId], StringComparer.Ordinal);

    private static bool IsLookupToForm(FormFieldDefinition field, Guid targetFormId) =>
        field.Type == FormFieldTypes.RecordLookup
        && field.Lookup is not null
        && field.Lookup.SourceType == "form_records"
        && Guid.TryParse(field.Lookup.SourceFormId, out var configuredTargetFormId)
        && configuredTargetFormId == targetFormId;

    private static bool TryGuid(object? value, out Guid id)
    {
        id = Guid.Empty;
        var text = value switch
        {
            string item => item,
            JsonElement { ValueKind: JsonValueKind.String } item => item.GetString(),
            _ => null
        };
        return Guid.TryParse(text, out id);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schema)
    {
        try { return schema?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument values) =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(values.RootElement.GetRawText(), JsonOptions)
        ?? new Dictionary<string, object?>();

    private static RecordQueryException PanelNotFound() =>
        new(StatusCodes.Status404NotFound, "Related-record panel was not found.");

    private sealed record VersionedDefinition(
        FormDefinition Form,
        int VersionNumber,
        FormSchemaDefinition Schema,
        FormFieldDefinition Field);

    private sealed record RelatedPanelDefinition(
        Guid SourceFormId,
        string SourceFormName,
        string SourceFieldId,
        string SourceFieldLabel,
        FormSchemaDefinition PreviewSchema);

    private sealed record PermittedPanel(
        RelatedPanelDefinition Definition,
        IReadOnlyList<RelatedRecordColumnDto> Columns);
}
