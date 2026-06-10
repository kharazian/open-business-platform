using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IncomingWebhookExecutionService
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly RecordSubmissionService recordSubmission;
    private readonly RecordMutationService recordMutation;
    private readonly PermissionService permissionService;
    private readonly IntegrationLogService integrationLogs;
    private readonly IncomingWebhookListenerSecretHasher secretHasher;

    public IncomingWebhookExecutionService(
        OpenBusinessPlatformDbContext dbContext,
        RecordSubmissionService recordSubmission,
        RecordMutationService recordMutation,
        PermissionService permissionService,
        IntegrationLogService integrationLogs,
        IncomingWebhookListenerSecretHasher secretHasher)
    {
        this.dbContext = dbContext;
        this.recordSubmission = recordSubmission;
        this.recordMutation = recordMutation;
        this.permissionService = permissionService;
        this.integrationLogs = integrationLogs;
        this.secretHasher = secretHasher;
    }

    public async Task<ReceiveIncomingWebhookResponse> ReceiveAsync(
        ClaimsPrincipal principal,
        string listenerKey,
        IReadOnlyDictionary<string, object?> payload,
        string? rawListenerSecret,
        CancellationToken cancellationToken)
    {
        var listener = await dbContext.IncomingWebhookListeners
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.ListenerKey == listenerKey, cancellationToken);

        if (listener is null || !listener.IsActive)
        {
            throw new IncomingWebhookException(StatusCodes.Status404NotFound, "Incoming webhook listener was not found.");
        }

        await EnsureAuthenticatedAsync(principal, listener, rawListenerSecret, cancellationToken);

        var startedAt = DateTimeOffset.UtcNow;
        var integrationKey = principal.FindFirstValue(IntegrationApiKeyClaims.IntegrationKey) ?? listener.ListenerKey;
        var createdById = GetLinkedUserId(principal) ?? listener.CreatedById;
        var requestMetadata = new Dictionary<string, object?>
        {
            ["listenerKey"] = listener.ListenerKey,
            ["authMode"] = listener.AuthMode,
            ["action"] = listener.Action,
            ["mappedFieldCount"] = IncomingWebhookListenerService.DeserializeMapping(listener.MappingJson).FieldMappings.Count
        };

        try
        {
            var mapping = IncomingWebhookListenerService.DeserializeMapping(listener.MappingJson);
            var values = IncomingWebhookPayloadMapper.MapValues(mapping, payload);

            var effectivePrincipal = CreateEffectivePrincipal(principal, listener);
            var recordId = string.Equals(listener.Action, IncomingWebhookListenerActions.Upsert, StringComparison.Ordinal)
                ? await UpsertRecordAsync(listener, values, effectivePrincipal, createdById, cancellationToken)
                : await CreateRecordAsync(listener, values, effectivePrincipal, createdById, cancellationToken);

            var log = await RecordLogAsync(
                listener,
                integrationKey,
                IntegrationLogStatuses.Succeeded,
                "Record",
                recordId,
                createdById,
                requestMetadata,
                null,
                null,
                null,
                startedAt,
                cancellationToken);

            return new ReceiveIncomingWebhookResponse(IntegrationLogStatuses.Succeeded, recordId, log.Id);
        }
        catch (Exception exception) when (exception is IncomingWebhookException or RecordSubmissionException or RecordMutationException)
        {
            var (statusCode, errorCode) = exception switch
            {
                IncomingWebhookException incoming => (incoming.StatusCode, "webhook_receive_failed"),
                RecordSubmissionException record => (record.StatusCode, "record_validation_failed"),
                RecordMutationException record => (record.StatusCode, "record_update_failed"),
                _ => (StatusCodes.Status500InternalServerError, "webhook_receive_failed")
            };
            await RecordLogAsync(
                listener,
                integrationKey,
                IntegrationLogStatuses.Failed,
                "Form",
                listener.TargetFormId,
                createdById,
                requestMetadata,
                new Dictionary<string, object?> { ["statusCode"] = statusCode },
                errorCode,
                exception.Message,
                startedAt,
                cancellationToken);

            throw;
        }
    }

    private async Task<Guid> CreateRecordAsync(
        IncomingWebhookListener listener,
        IReadOnlyDictionary<string, object?> values,
        ClaimsPrincipal principal,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.CanAccessFormAsync(principal, listener.TargetFormId, PlatformPermissions.Form.Submit, cancellationToken))
        {
            throw new IncomingWebhookException(StatusCodes.Status403Forbidden, "Incoming webhook record access was denied.");
        }

        var record = await recordSubmission.SubmitRecordAsync(
            listener.TargetFormId,
            new SubmitRecordRequest(values),
            createdById,
            cancellationToken);

        return record.Id;
    }

    private async Task<Guid> UpsertRecordAsync(
        IncomingWebhookListener listener,
        IReadOnlyDictionary<string, object?> values,
        ClaimsPrincipal principal,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(listener.SafeLookupFieldId)
            || !values.TryGetValue(listener.SafeLookupFieldId, out var lookupValue)
            || lookupValue is null
            || string.IsNullOrWhiteSpace(lookupValue.ToString()))
        {
            throw new IncomingWebhookException(StatusCodes.Status400BadRequest, "Incoming webhook upsert lookup value is required.");
        }

        var matchingRecords = await dbContext.Records
            .AsNoTracking()
            .Where(record => record.FormId == listener.TargetFormId && !record.IsDeleted)
            .Select(record => new
            {
                record.Id,
                record.ConcurrencyStamp,
                record.ValuesJson
            })
            .ToArrayAsync(cancellationToken);
        var matches = matchingRecords
            .Where(record => ValuesMatch(record.ValuesJson, listener.SafeLookupFieldId, lookupValue))
            .ToArray();

        if (matches.Length == 0)
        {
            return await CreateRecordAsync(listener, values, principal, updatedById, cancellationToken);
        }

        if (matches.Length > 1)
        {
            throw new IncomingWebhookException(StatusCodes.Status409Conflict, "Incoming webhook lookup matched multiple records.");
        }

        var updated = await recordMutation.UpdateRecordAsync(
            matches[0].Id,
            new UpdateRecordRequest(values, matches[0].ConcurrencyStamp),
            principal,
            updatedById,
            permissionService,
            cancellationToken);

        return updated.Id;
    }

    private static bool ValuesMatch(JsonDocument valuesJson, string fieldId, object lookupValue)
    {
        var values = valuesJson.RootElement.Deserialize<Dictionary<string, object?>>(IncomingWebhookJson.Options)
            ?? new Dictionary<string, object?>();

        if (!values.TryGetValue(fieldId, out var currentValue))
        {
            return false;
        }

        return string.Equals(
            NormalizeLookupValue(currentValue),
            NormalizeLookupValue(lookupValue),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeLookupValue(object? value)
    {
        return value is JsonElement element
            ? element.ToString()?.Trim()
            : value?.ToString()?.Trim();
    }

    private async Task EnsureAuthenticatedAsync(
        ClaimsPrincipal principal,
        IncomingWebhookListener listener,
        string? rawListenerSecret,
        CancellationToken cancellationToken)
    {
        if (string.Equals(listener.AuthMode, IncomingWebhookListenerAuthModes.ListenerSecret, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(rawListenerSecret)
                || string.IsNullOrWhiteSpace(listener.SecretHash)
                || !secretHasher.Verify(rawListenerSecret, listener.SecretHash))
            {
                throw new IncomingWebhookException(StatusCodes.Status401Unauthorized, "Incoming webhook listener secret is invalid.");
            }

            return;
        }

        if (!PublicRecordApiAccess.HasScope(principal, IntegrationApiKeyScopes.WebhooksReceive))
        {
            throw new IncomingWebhookException(StatusCodes.Status403Forbidden, "API key does not have the required webhook scope.");
        }

        _ = PublicRecordApiAccess.CreateEffectiveUserPrincipal(principal);
        await Task.CompletedTask;
    }

    private static ClaimsPrincipal CreateEffectivePrincipal(ClaimsPrincipal principal, IncomingWebhookListener listener)
    {
        if (string.Equals(listener.AuthMode, IncomingWebhookListenerAuthModes.ApiKey, StringComparison.Ordinal))
        {
            return PublicRecordApiAccess.CreateEffectiveUserPrincipal(principal);
        }

        if (listener.CreatedById is null)
        {
            throw new IncomingWebhookException(StatusCodes.Status403Forbidden, "Listener secret webhooks require a linked creator for permission checks.");
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, listener.CreatedById.Value.ToString()) },
            "IncomingWebhookListenerSecret"));
    }

    private async Task<IntegrationLogDto> RecordLogAsync(
        IncomingWebhookListener listener,
        string integrationKey,
        string status,
        string targetType,
        Guid targetId,
        Guid? createdById,
        IReadOnlyDictionary<string, object?> requestMetadata,
        IReadOnlyDictionary<string, object?>? responseMetadata,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        return await integrationLogs.RecordAsync(
            new RecordIntegrationLogRequest(
                IntegrationLogDirections.Inbound,
                IntegrationLogTypes.Webhook,
                integrationKey,
                status,
                "IncomingWebhookListener",
                SourceId: listener.Id,
                TargetEntityType: targetType,
                TargetEntityId: targetId,
                AttemptCount: 1,
                MaxAttempts: 1,
                IsRetryable: false,
                RequestMetadata: requestMetadata,
                ResponseMetadata: responseMetadata,
                ErrorCode: errorCode,
                ErrorMessage: errorMessage,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow),
            createdById,
            cancellationToken);
    }

    private static Guid? GetLinkedUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(IntegrationApiKeyClaims.CreatedByUserId);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
