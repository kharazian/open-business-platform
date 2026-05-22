using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

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
        Guid formId,
        ListRecordsRequest request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = Normalize(request.Search);

        var records = await dbContext.Records
            .AsNoTracking()
            .Where(record => record.FormId == formId && !record.IsDeleted)
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .ToArrayAsync(cancellationToken);

        var filteredRecords = string.IsNullOrWhiteSpace(search)
            ? records.Select(ToListItem)
            : records.Select(ToListItem).Where(record => MatchesSearch(record, search));

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

    public async Task<FormRecordDetailDto> GetRecordAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var record = await dbContext.Records
            .AsNoTracking()
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Record was not found.");
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

        return new FormRecordDetailDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            DeserializeValues(record.ValuesJson),
            schema,
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById,
            record.UpdatedAt,
            record.UpdatedById);
    }

    private static FormRecordListItemDto ToListItem(FormRecord record)
    {
        return new FormRecordListItemDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            DeserializeValues(record.ValuesJson),
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
