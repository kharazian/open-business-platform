using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Triggers;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordSubmissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerEventOutbox triggerEventOutbox;
    private readonly RecordLookupService recordLookup;
    private readonly AutonumberService autonumbers;
    private readonly FileAttachmentService attachments;
    private readonly RecordRelationshipService relationships;

    public RecordSubmissionService(
        OpenBusinessPlatformDbContext dbContext,
        TriggerEventOutbox triggerEventOutbox,
        RecordLookupService recordLookup,
        AutonumberService autonumbers,
        FileAttachmentService attachments,
        RecordRelationshipService relationships)
    {
        this.dbContext = dbContext;
        this.triggerEventOutbox = triggerEventOutbox;
        this.recordLookup = recordLookup;
        this.autonumbers = autonumbers;
        this.attachments = attachments;
        this.relationships = relationships;
    }

    public async Task<FormRecordDto> SubmitRecordAsync(
        Guid formId,
        SubmitRecordRequest request,
        ClaimsPrincipal principal,
        Guid? submittedById,
        PermissionService permissionService,
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

        var values = new Dictionary<string, object?>(request.Values, StringComparer.Ordinal);
        await autonumbers.ApplyAsync(form.Id, schema, values, cancellationToken);
        var validation = FormSchemaValidator.ValidateRecordValues(schema, values);
        if (!validation.Valid)
        {
            throw new RecordSubmissionException(StatusCodes.Status400BadRequest, "Record values are invalid.", validation.Errors);
        }

        var lookupValidation = await recordLookup.ValidateLookupValuesAsync(
            principal,
            schema,
            values,
            null,
            permissionService,
            cancellationToken);
        if (lookupValidation.Count > 0)
        {
            throw new RecordSubmissionException(StatusCodes.Status400BadRequest, "Record values are invalid.", lookupValidation);
        }

        var record = new FormRecord
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            FormVersionId = form.CurrentVersion.Id,
            Status = RecordStatuses.Active,
            OwnerId = submittedById,
            ValuesJson = JsonSerializer.SerializeToDocument(values, JsonOptions),
            CreatedById = submittedById
        };

        dbContext.Records.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        var attachmentValidation = await attachments.ValidateAndClaimAsync(record.Id, form.Id, form.CurrentVersion.Id, schema, null, values, submittedById, cancellationToken);
        if (attachmentValidation.Count > 0)
            throw new RecordSubmissionException(StatusCodes.Status400BadRequest, "Record values are invalid.", attachmentValidation);
        await relationships.SynchronizeAsync(record.Id, record.FormId, record.FormVersionId, schema, values, submittedById, cancellationToken);

        AddAudit(record, submittedById);
        var snapshot = ToTriggerSnapshot(record, values);
        triggerEventOutbox.Enqueue(new TriggerEventContext(
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
            DateTimeOffset.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var displayValues = await recordLookup.ResolveLookupDisplayValuesAsync(
            principal,
            schema,
            new[] { (IReadOnlyDictionary<string, object?>)values },
            permissionService,
            cancellationToken);

        return ToDto(record, values, displayValues[0]);
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

    private static FormRecordDto ToDto(
        FormRecord record,
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyDictionary<string, string> displayValues)
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
            record.CreatedById,
            displayValues);
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
