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

    public RecordQueryService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
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

        var filteredRecords = string.IsNullOrWhiteSpace(search)
            ? records.Select(record => ToListItem(record, fieldAccess.HiddenFieldIds))
            : records.Select(record => ToListItem(record, fieldAccess.HiddenFieldIds)).Where(record => MatchesSearch(record, search));

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

        return new FormRecordDetailDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            MaskValues(DeserializeValues(record.ValuesJson), fieldAccess.HiddenFieldIds),
            visibleSchema,
            fieldAccess.ReadOnlyFieldIds.ToArray(),
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById,
            record.UpdatedAt,
            record.UpdatedById);
    }

    private static FormRecordListItemDto ToListItem(FormRecord record, IReadOnlySet<string> hiddenFieldIds)
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
            MaskValues(DeserializeValues(record.ValuesJson), hiddenFieldIds),
            record.CreatedAt,
            record.CreatedById);
    }

    private static bool MatchesSearch(FormRecordListItemDto record, string search)
    {
        return record.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.FormVersionId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.Status.Contains(search, StringComparison.OrdinalIgnoreCase)
            || record.Values.Any(pair =>
                pair.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                || Convert.ToString(pair.Value)?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
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

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
