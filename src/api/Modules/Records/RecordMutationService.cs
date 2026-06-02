using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

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
        ClaimsPrincipal principal,
        Guid? updatedById,
        PermissionService permissionService,
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

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.Edit, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status403Forbidden, "Record access was denied.");
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

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, record.FormId, cancellationToken);
        var currentValues = DeserializeValues(record.ValuesJson);
        var effectiveValues = MergeProtectedValues(currentValues, request.Values, fieldAccess);

        var validation = FormSchemaValidator.ValidateRecordValues(schema, effectiveValues);
        if (!validation.Valid)
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Record values are invalid.", validation.Errors);
        }

        record.ValuesJson = JsonSerializer.SerializeToDocument(effectiveValues, JsonOptions);
        record.UpdatedById = updatedById;
        AddAudit(record.Id, "record_updated", updatedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDetailDto(record, MaskValues(effectiveValues, fieldAccess.HiddenFieldIds), schema, fieldAccess.ReadOnlyFieldIds);
    }

    public async Task<bool> DeleteRecordAsync(
        Guid recordId,
        ClaimsPrincipal principal,
        Guid? deletedById,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var record = await dbContext.Records
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            return false;
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.Delete, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        record.Status = RecordStatuses.Deleted;
        record.DeletedById = deletedById;
        AddAudit(record.Id, "record_deleted", deletedById);
        dbContext.Records.Remove(record);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<FormRecordDetailDto> AssignRecordAsync(
        Guid recordId,
        AssignRecordRequest request,
        ClaimsPrincipal principal,
        Guid? updatedById,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var record = await dbContext.Records
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordMutationException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.Assign, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        EnsureConcurrencyStamp(record.ConcurrencyStamp, request.ConcurrencyStamp);
        await EnsureAssignmentTargetsAsync(request.AssignedToUserId, request.AssignedGroupId, cancellationToken);

        record.AssignedToUserId = NormalizeNullableId(request.AssignedToUserId);
        record.AssignedGroupId = NormalizeNullableId(request.AssignedGroupId);
        record.UpdatedById = updatedById;
        AddAudit(record.Id, "record_assigned", updatedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToDetailDtoAsync(record, principal, permissionService, cancellationToken);
    }

    public async Task<FormRecordDetailDto> ChangeStatusAsync(
        Guid recordId,
        ChangeRecordStatusRequest request,
        ClaimsPrincipal principal,
        Guid? updatedById,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Record status is required.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var record = await dbContext.Records
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordMutationException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.ChangeStatus, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        EnsureConcurrencyStamp(record.ConcurrencyStamp, request.ConcurrencyStamp);
        record.Status = request.Status.Trim();
        record.UpdatedById = updatedById;
        AddAudit(record.Id, "record_status_changed", updatedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToDetailDtoAsync(record, principal, permissionService, cancellationToken);
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
        FormSchemaDefinition schema,
        IReadOnlyCollection<string> readOnlyFieldIds)
    {
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
            schema,
            readOnlyFieldIds,
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

    private async Task<FormRecordDetailDto> ToDetailDtoAsync(
        FormRecord record,
        ClaimsPrincipal principal,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (record.FormVersion is null)
        {
            throw new RecordMutationException(StatusCodes.Status409Conflict, "Record form version was not found.");
        }

        var schema = DeserializeSchema(record.FormVersion.SchemaJson);
        if (schema is null)
        {
            throw new RecordMutationException(StatusCodes.Status409Conflict, "Record form version schema is invalid.");
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, record.FormId, cancellationToken);
        return ToDetailDto(
            record,
            MaskValues(DeserializeValues(record.ValuesJson), fieldAccess.HiddenFieldIds),
            schema,
            fieldAccess.ReadOnlyFieldIds);
    }

    private async Task EnsureAssignmentTargetsAsync(Guid? assignedToUserId, Guid? assignedGroupId, CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizeNullableId(assignedToUserId);
        var normalizedGroupId = NormalizeNullableId(assignedGroupId);

        if (normalizedUserId is not null
            && !await dbContext.Users.AnyAsync(user => user.Id == normalizedUserId.Value && user.IsActive, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Assigned user was not found.");
        }

        if (normalizedGroupId is not null
            && !await dbContext.Groups.AnyAsync(group => group.Id == normalizedGroupId.Value && group.IsActive, cancellationToken))
        {
            throw new RecordMutationException(StatusCodes.Status400BadRequest, "Assigned group was not found.");
        }
    }

    private static IReadOnlyDictionary<string, object?> MergeProtectedValues(
        IReadOnlyDictionary<string, object?> currentValues,
        IReadOnlyDictionary<string, object?> requestedValues,
        FieldAccessResult fieldAccess)
    {
        var merged = requestedValues.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var blocked = fieldAccess.HiddenFieldIds
            .Concat(fieldAccess.ReadOnlyFieldIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var fieldId in blocked)
        {
            var current = currentValues.TryGetValue(fieldId, out var currentValue)
                ? JsonSerializer.Serialize(currentValue, JsonOptions)
                : null;
            var requested = requestedValues.TryGetValue(fieldId, out var requestedValue)
                ? JsonSerializer.Serialize(requestedValue, JsonOptions)
                : null;

            if (requested is not null && !string.Equals(current, requested, StringComparison.Ordinal))
            {
                throw new RecordMutationException(StatusCodes.Status403Forbidden, $"Field '{fieldId}' cannot be changed.");
            }

            if (currentValues.TryGetValue(fieldId, out var protectedValue))
            {
                merged[fieldId] = protectedValue;
            }
            else
            {
                merged.Remove(fieldId);
            }
        }

        return merged;
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

    private static Guid? NormalizeNullableId(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
    }
}
