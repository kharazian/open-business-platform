using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Triggers;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordSubmissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerEventDispatcher triggerDispatcher;

    public RecordSubmissionService(OpenBusinessPlatformDbContext dbContext, TriggerEventDispatcher triggerDispatcher)
    {
        this.dbContext = dbContext;
        this.triggerDispatcher = triggerDispatcher;
    }

    public async Task<FormRecordDto> SubmitRecordAsync(
        Guid formId,
        SubmitRecordRequest request,
        Guid? submittedById,
        CancellationToken cancellationToken)
    {
        if (request.Values is null)
        {
            throw new RecordSubmissionException(
                StatusCodes.Status400BadRequest,
                "Record values are required.",
                new[] { new FormValidationError("values", "record.values_required", "Record values are required.") });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new RecordSubmissionException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        if (!string.Equals(form.Status, FormStatuses.Published, StringComparison.Ordinal)
            || form.CurrentVersionId is null
            || form.CurrentVersion is null)
        {
            throw new RecordSubmissionException(StatusCodes.Status409Conflict, "Only published forms can accept records.");
        }

        var schema = DeserializeSchema(form.CurrentVersion.SchemaJson);
        if (schema is null)
        {
            throw new RecordSubmissionException(StatusCodes.Status409Conflict, "Published form version schema is invalid.");
        }

        var validation = FormSchemaValidator.ValidateRecordValues(schema, request.Values);
        if (!validation.Valid)
        {
            throw new RecordSubmissionException(StatusCodes.Status400BadRequest, "Record values are invalid.", validation.Errors);
        }

        var record = new FormRecord
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            FormVersionId = form.CurrentVersion.Id,
            Status = RecordStatuses.Active,
            OwnerId = submittedById,
            ValuesJson = JsonSerializer.SerializeToDocument(request.Values, JsonOptions),
            CreatedById = submittedById
        };

        dbContext.Records.Add(record);
        AddAudit(record, submittedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var snapshot = ToTriggerSnapshot(record, request.Values);
        await triggerDispatcher.DispatchAsync(new TriggerEventContext(
            TriggerEvents.RecordCreated,
            record.FormId,
            record.Id,
            submittedById,
            null,
            snapshot,
            Array.Empty<string>(),
            null,
            record.Status,
            null,
            record.AssignedToUserId,
            null,
            record.AssignedGroupId,
            DateTimeOffset.UtcNow), cancellationToken);

        return ToDto(record, request.Values);
    }

    private void AddAudit(FormRecord record, Guid? userId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = record.Id,
            Action = "record_created",
            UserId = userId
        });
    }

    private static FormRecordDto ToDto(FormRecord record, IReadOnlyDictionary<string, object?> values)
    {
        return new FormRecordDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            values,
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById);
    }

    private static TriggerRecordSnapshot ToTriggerSnapshot(FormRecord record, IReadOnlyDictionary<string, object?> values)
    {
        return new TriggerRecordSnapshot(
            record.Id,
            record.FormId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            values);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }
}
