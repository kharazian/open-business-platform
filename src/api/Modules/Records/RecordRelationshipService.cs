using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record RecordRelationshipEdge(string FieldId, Guid TargetFormId, Guid TargetRecordId);

public sealed class RecordRelationshipService(OpenBusinessPlatformDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SynchronizeAsync(Guid sourceRecordId, Guid sourceFormId, Guid sourceFormVersionId, FormSchemaDefinition schema, IReadOnlyDictionary<string, object?> values, Guid? actorId, CancellationToken ct)
    {
        var expected = ExtractEdges(schema, values).ToDictionary(edge => edge.FieldId, StringComparer.Ordinal);
        var existing = await dbContext.RecordRelationships.Where(item => item.SourceRecordId == sourceRecordId).ToArrayAsync(ct);
        foreach (var relationship in existing)
        {
            if (!expected.Remove(relationship.SourceFieldId, out var edge))
            {
                dbContext.RecordRelationships.Remove(relationship);
                continue;
            }
            if (relationship.TargetFormId == edge.TargetFormId && relationship.TargetRecordId == edge.TargetRecordId) continue;
            relationship.TargetFormId = edge.TargetFormId;
            relationship.TargetRecordId = edge.TargetRecordId;
            relationship.UpdatedById = actorId;
        }
        foreach (var edge in expected.Values)
        {
            dbContext.RecordRelationships.Add(new RecordRelationship
            {
                Id = Guid.NewGuid(), SourceFormId = sourceFormId, SourceFormVersionId = sourceFormVersionId, SourceRecordId = sourceRecordId,
                SourceFieldId = edge.FieldId, TargetFormId = edge.TargetFormId, TargetRecordId = edge.TargetRecordId, CreatedById = actorId
            });
        }
    }

    public async Task RemoveOutgoingAsync(Guid sourceRecordId, CancellationToken ct)
    {
        var outgoing = await dbContext.RecordRelationships.Where(item => item.SourceRecordId == sourceRecordId).ToArrayAsync(ct);
        dbContext.RecordRelationships.RemoveRange(outgoing);
    }

    public async Task EnsureTargetCanBeDeletedAsync(FormRecord target, CancellationToken ct)
    {
        var sources = (await dbContext.RecordRelationships.AsNoTracking()
            .Where(item => item.TargetRecordId == target.Id && item.SourceRecordId != target.Id && item.SourceRecord != null && !item.SourceRecord.IsDeleted)
            .Select(item => item.SourceRecordId).Distinct().ToArrayAsync(ct)).ToHashSet();

        await foreach (var source in dbContext.Records.AsNoTracking().Include(item => item.FormVersion)
            .Where(item => item.Id != target.Id && !item.IsDeleted && !sources.Contains(item.Id)).AsAsyncEnumerable().WithCancellation(ct))
        {
            if (source.FormVersion is null) continue;
            var schema = DeserializeSchema(source.FormVersion.SchemaJson);
            if (schema is null) continue;
            var values = DeserializeValues(source.ValuesJson);
            if (ExtractEdges(schema, values).Any(edge => edge.TargetRecordId == target.Id)) sources.Add(source.Id);
        }

        if (sources.Count > 0)
            throw new RecordMutationException(StatusCodes.Status409Conflict, $"Record cannot be deleted because {sources.Count} active record(s) reference it.");
    }

    public static IReadOnlyList<RecordRelationshipEdge> ExtractEdges(FormSchemaDefinition schema, IReadOnlyDictionary<string, object?> values)
    {
        var edges = new List<RecordRelationshipEdge>();
        foreach (var field in schema.Fields.Where(item => item.Type == FormFieldTypes.RecordLookup && item.Lookup is not null))
        {
            if (!Guid.TryParse(field.Lookup!.SourceFormId, out var targetFormId) || !values.TryGetValue(field.Id, out var value) || !TryGuid(value, out var targetRecordId)) continue;
            edges.Add(new(field.Id, targetFormId, targetRecordId));
        }
        return edges;
    }

    private static bool TryGuid(object? value, out Guid id)
    {
        id = Guid.Empty;
        var text = value switch { string item => item, JsonElement { ValueKind: JsonValueKind.String } item => item.GetString(), _ => null };
        return Guid.TryParse(text, out id);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument schema) { try { return JsonSerializer.Deserialize<FormSchemaDefinition>(schema.RootElement.GetRawText(), JsonOptions); } catch (JsonException) { return null; } }
    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument values) => JsonSerializer.Deserialize<Dictionary<string, object?>>(values.RootElement.GetRawText(), JsonOptions) ?? new Dictionary<string, object?>();
}
