using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordMutationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public RecordMutationService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<FormRecordDetailDto> UpdateRecordAsync(
        Guid recordId,
        UpdateRecordRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        if (request.Values is null)
        {
            throw new RecordMutationException(
                StatusCodes.Status400BadRequest,
                "Record values are required.",
                new[] { new FormValidationError("values", "record.values_required", "Record values are required.") });
        }

        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp))
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Record concurrency stamp is required.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var record = await dbContext.Records
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordMutationException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        EnsureConcurrencyStamp(record.ConcurrencyStamp, request.ConcurrencyStamp);

        if (record.FormVersion is null)
        {
            throw new RecordMutationException(StatusCodes.Status409Conflict, "Record form version was not found.");
        }

        var schema = DeserializeSchema(record.FormVersion.SchemaJson);
        if (schema is null)
        {
            throw new RecordMutationException(StatusCodes.Status409Conflict, "Record form version schema is invalid.");
        }

        var validation = FormSchemaValidator.ValidateRecordValues(schema, request.Values);
        if (!validation.Valid)
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Record values are invalid.", validation.Errors);
        }

        record.ValuesJson = JsonSerializer.SerializeToDocument(request.Values, JsonOptions);
        record.UpdatedById = updatedById;
        AddAudit(record.Id, "record_updated", updatedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDetailDto(record, request.Values, schema);
    }

    public async Task<bool> DeleteRecordAsync(Guid recordId, Guid? deletedById, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var record = await dbContext.Records
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            return false;
        }

        record.Status = RecordStatuses.Deleted;
        record.DeletedById = deletedById;
        AddAudit(record.Id, "record_deleted", deletedById);
        dbContext.Records.Remove(record);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new RecordMutationException(StatusCodes.Status409Conflict, "The record was changed by another user.");
        }
    }

    private void AddAudit(Guid recordId, string action, Guid? userId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            EntityType = "Record",
            EntityId = recordId,
            Action = action,
            UserId = userId
        });
    }

    private static FormRecordDetailDto ToDetailDto(
        FormRecord record,
        IReadOnlyDictionary<string, object?> values,
        FormSchemaDefinition schema)
    {
        return new FormRecordDetailDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            values,
            schema,
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById,
            record.UpdatedAt,
            record.UpdatedById);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }
}
