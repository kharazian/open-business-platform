using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly RecordLookupService recordLookup;

    public RecordQueryService(OpenBusinessPlatformDbContext dbContext, RecordLookupService recordLookup)
    {
        this.dbContext = dbContext;
        this.recordLookup = recordLookup;
    }

    public async Task<PagedResultDto<FormRecordListItemDto>> ListRecordsAsync(
        ClaimsPrincipal principal,
        Guid formId,
        ListRecordsRequest request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = Normalize(request.Search);
        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, formId, cancellationToken);
        var query = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == formId && !record.IsDeleted),
            formId,
            PlatformPermissions.Form.View,
            cancellationToken);

        var records = await query
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .ToArrayAsync(cancellationToken);
        var schema = await GetCurrentFormSchemaAsync(formId, cancellationToken);
        var visibleSchema = schema is null ? null : RemoveHiddenFieldsFromSchema(schema, fieldAccess.HiddenFieldIds);
        var recordValues = records
            .Select(record => MaskValues(DeserializeValues(record.ValuesJson), fieldAccess.HiddenFieldIds))
            .ToArray();
        var displayValues = visibleSchema is null
            ? recordValues.Select(_ => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)).ToArray()
            : await recordLookup.ResolveLookupDisplayValuesAsync(principal, visibleSchema, recordValues, permissionService, cancellationToken);
        var recordItems = records
            .Select((record, index) => ToListItem(record, recordValues[index], displayValues[index]))
            .ToArray();

        var filteredRecords = string.IsNullOrWhiteSpace(search)
            ? recordItems
            : recordItems.Where(record => MatchesSearch(record, search));

        var filtered = filteredRecords.ToArray();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return new PagedResultDto<FormRecordListItemDto>(filtered.LongLength, items);
    }

    public async Task<Guid?> GetRecordFormIdAsync(Guid recordId, CancellationToken cancellationToken)
    {
        return await dbContext.Records
            .AsNoTracking()
            .Where(record => record.Id == recordId && !record.IsDeleted)
            .Select(record => (Guid?)record.FormId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormRecordDetailDto> GetRecordAsync(
        ClaimsPrincipal principal,
        Guid recordId,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.Records
            .AsNoTracking()
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        if (record.FormVersion is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Record form version was not found.");
        }

        var schema = DeserializeSchema(record.FormVersion.SchemaJson);
        if (schema is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Record form version schema is invalid.");
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, record.FormId, cancellationToken);
        var visibleSchema = RemoveHiddenFieldsFromSchema(schema, fieldAccess.HiddenFieldIds);
        var values = MaskValues(DeserializeValues(record.ValuesJson), fieldAccess.HiddenFieldIds);
        var displayValues = await recordLookup.ResolveLookupDisplayValuesAsync(
            principal,
            visibleSchema,
            new[] { values },
            permissionService,
            cancellationToken);

        return new FormRecordDetailDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            values,
            visibleSchema,
            fieldAccess.ReadOnlyFieldIds.ToArray(),
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById,
            record.UpdatedAt,
            record.UpdatedById,
            displayValues[0]);
    }

    public async Task<SubTableRowsDto> ListSubTableRowsAsync(
        ClaimsPrincipal principal,
        Guid recordId,
        string fieldId,
        ListSubTableRowsRequest request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var parentRecord = await dbContext.Records
            .AsNoTracking()
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (parentRecord is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, parentRecord, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        if (parentRecord.FormVersion is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Record form version was not found.");
        }

        var parentSchema = DeserializeSchema(parentRecord.FormVersion.SchemaJson);
        if (parentSchema is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Record form version schema is invalid.");
        }

        var parentFieldAccess = await permissionService.GetFieldAccessAsync(principal, parentRecord.FormId, cancellationToken);
        if (parentFieldAccess.HiddenFieldIds.Contains(fieldId))
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Sub-table field was not found.");
        }

        var subTableField = parentSchema.Fields.FirstOrDefault(field =>
            string.Equals(field.Id, fieldId, StringComparison.Ordinal)
            && string.Equals(field.Type, FormFieldTypes.SubTable, StringComparison.Ordinal));

        if (subTableField?.SubTable is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Sub-table field was not found.");
        }

        if (!Guid.TryParse(subTableField.SubTable.ChildFormId, out var childFormId))
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Sub-table child form is invalid.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, childFormId, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Sub-table child form access was denied.");
        }

        var childForm = await dbContext.Forms
            .AsNoTracking()
            .Include(form => form.CurrentVersion)
            .FirstOrDefaultAsync(form => form.Id == childFormId && !form.IsDeleted, cancellationToken);

        if (childForm?.CurrentVersion is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Sub-table child form was not found.");
        }

        var childSchema = DeserializeSchema(childForm.CurrentVersion.SchemaJson);
        if (childSchema is null)
        {
            throw new RecordQueryException(StatusCodes.Status409Conflict, "Sub-table child form schema is invalid.");
        }

        var childFieldAccess = await permissionService.GetFieldAccessAsync(principal, childFormId, cancellationToken);
        var visibleChildSchema = RemoveHiddenFieldsFromSchema(childSchema, childFieldAccess.HiddenFieldIds);
        var childFieldsById = visibleChildSchema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var displayColumnFieldIds = subTableField.SubTable.DisplayColumnFieldIds
            .Where(fieldId => !childFieldAccess.HiddenFieldIds.Contains(fieldId))
            .Where(fieldId => childFieldsById.TryGetValue(fieldId, out var field) && !string.Equals(field.Type, FormFieldTypes.SubTable, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var columns = displayColumnFieldIds
            .Select(fieldId => childFieldsById[fieldId])
            .Select(field => new SubTableColumnDto(field.Id, field.Label, field.Type))
            .ToArray();

        var childQuery = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == childFormId && !record.IsDeleted),
            childFormId,
            PlatformPermissions.Form.View,
            cancellationToken);

        var linkedRows = (await childQuery
                .OrderByDescending(record => record.CreatedAt)
                .ThenByDescending(record => record.Id)
                .ToArrayAsync(cancellationToken))
            .Select(record => new ChildRecordRow(record, DeserializeValues(record.ValuesJson)))
            .Where(row => IsLinkedToParentRecord(row.Values, subTableField.SubTable.ParentLookupFieldId, recordId))
            .ToArray();
        var pagedRows = linkedRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        var rowValues = pagedRows
            .Select(row => ProjectValues(row.Values, displayColumnFieldIds))
            .ToArray();
        var displayValues = await recordLookup.ResolveLookupDisplayValuesAsync(
            principal,
            visibleChildSchema,
            rowValues,
            permissionService,
            cancellationToken);
        var rowDtos = pagedRows
            .Select((row, index) => new SubTableRowDto(
                row.Record.Id,
                rowValues[index],
                ProjectDisplayValues(displayValues[index], displayColumnFieldIds),
                row.Record.CreatedAt))
            .ToArray();

        return new SubTableRowsDto(fieldId, columns, linkedRows.LongLength, rowDtos);
    }

    public static bool IsLinkedToParentRecord(
        IReadOnlyDictionary<string, object?> values,
        string parentLookupFieldId,
        Guid parentRecordId)
    {
        return values.TryGetValue(parentLookupFieldId, out var value)
            && string.Equals(Normalize(ToDisplayString(value)), parentRecordId.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static FormRecordListItemDto ToListItem(
        FormRecord record,
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyDictionary<string, string> displayValues)
    {
        return new FormRecordListItemDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            values,
            record.CreatedAt,
            record.CreatedById,
            displayValues);
    }

    private static bool MatchesSearch(FormRecordListItemDto record, string search)
    {
        return record.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.FormVersionId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.Status.Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.Values.Any(pair =>
                pair.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                || Convert.ToString(pair.Value)?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
            || record.DisplayValues?.Any(pair =>
                pair.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                || pair.Value.Contains(search, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private async Task<FormSchemaDefinition?> GetCurrentFormSchemaAsync(Guid formId, CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        return DeserializeSchema(form?.CurrentVersion?.SchemaJson);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static IReadOnlyDictionary<string, object?> MaskValues(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlySet<string> hiddenFieldIds)
    {
        if (hiddenFieldIds.Count == 0)
        {
            return values;
        }

        return values
            .Where(pair => !hiddenFieldIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, object?> ProjectValues(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyCollection<string> fieldIds)
    {
        return fieldIds
            .Where(fieldId => values.ContainsKey(fieldId))
            .ToDictionary(fieldId => fieldId, fieldId => values[fieldId], StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> ProjectDisplayValues(
        IReadOnlyDictionary<string, string> displayValues,
        IReadOnlyCollection<string> fieldIds)
    {
        return displayValues
            .Where(pair => fieldIds.Contains(pair.Key, StringComparer.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static FormSchemaDefinition RemoveHiddenFieldsFromSchema(
        FormSchemaDefinition schema,
        IReadOnlySet<string> hiddenFieldIds)
    {
        if (hiddenFieldIds.Count == 0)
        {
            return schema;
        }

        return schema with
        {
            Fields = schema.Fields
                .Where(field => !hiddenFieldIds.Contains(field.Id))
                .ToArray(),
            Layout = RemoveHiddenFieldsFromLayout(schema.Layout, hiddenFieldIds)
        };
    }

    private static FormLayoutDefinition RemoveHiddenFieldsFromLayout(
        FormLayoutDefinition layout,
        IReadOnlySet<string> hiddenFieldIds)
    {
        return layout with
        {
            Pages = layout.Pages.Select(page => page with
            {
                Sections = page.Sections.Select(section => section with
                {
                    Rows = section.Rows.Select(row => row with
                    {
                        Columns = row.Columns.Select(column => column with
                        {
                            Fields = column.Fields
                                .Where(fieldId => !hiddenFieldIds.Contains(fieldId))
                                .ToArray()
                        }).ToArray()
                    }).ToArray()
                }).ToArray()
            }).ToArray()
        };
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }

    private static string? ToDisplayString(object? value)
    {
        return value switch
        {
            null => null,
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonElement element => element.ToString(),
            _ => Convert.ToString(value)
        };
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record ChildRecordRow(
        FormRecord Record,
        IReadOnlyDictionary<string, object?> Values);
}
