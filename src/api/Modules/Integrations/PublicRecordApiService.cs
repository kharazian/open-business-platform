using System.Security.Claims;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class PublicRecordApiService
{
    private readonly RecordSubmissionService recordSubmission;
    private readonly RecordQueryService recordQuery;
    private readonly PermissionService permissionService;
    private readonly IntegrationLogService integrationLogs;

    public PublicRecordApiService(
        RecordSubmissionService recordSubmission,
        RecordQueryService recordQuery,
        PermissionService permissionService,
        IntegrationLogService integrationLogs)
    {
        this.recordSubmission = recordSubmission;
        this.recordQuery = recordQuery;
        this.permissionService = permissionService;
        this.integrationLogs = integrationLogs;
    }

    public async Task<PagedResultDto<FormRecordListItemDto>> ListRecordsAsync(
        ClaimsPrincipal apiKeyPrincipal,
        Guid formId,
        ListRecordsRequest request,
        CancellationToken cancellationToken)
    {
        EnsureScope(apiKeyPrincipal, IntegrationApiKeyScopes.RecordsRead);
        var effectivePrincipal = PublicRecordApiAccess.CreateEffectiveUserPrincipal(apiKeyPrincipal);

        if (!await permissionService.CanAccessFormAsync(effectivePrincipal, formId, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status403Forbidden, "Record API access was denied.");
        }

        var records = await recordQuery.ListRecordsAsync(effectivePrincipal, formId, request, permissionService, cancellationToken);
        await RecordIntegrationLogAsync(apiKeyPrincipal, IntegrationLogStatuses.Succeeded, "Form", formId, cancellationToken);

        return records;
    }

    public async Task<FormRecordDetailDto> GetRecordAsync(
        ClaimsPrincipal apiKeyPrincipal,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        EnsureScope(apiKeyPrincipal, IntegrationApiKeyScopes.RecordsRead);
        var effectivePrincipal = PublicRecordApiAccess.CreateEffectiveUserPrincipal(apiKeyPrincipal);
        var record = await recordQuery.GetRecordAsync(effectivePrincipal, recordId, permissionService, cancellationToken);
        await RecordIntegrationLogAsync(apiKeyPrincipal, IntegrationLogStatuses.Succeeded, "Record", recordId, cancellationToken);

        return record;
    }

    public async Task<PublicRecordResponse> CreateRecordAsync(
        ClaimsPrincipal apiKeyPrincipal,
        Guid formId,
        PublicCreateRecordRequest request,
        CancellationToken cancellationToken)
    {
        EnsureScope(apiKeyPrincipal, IntegrationApiKeyScopes.RecordsCreate);
        var effectivePrincipal = PublicRecordApiAccess.CreateEffectiveUserPrincipal(apiKeyPrincipal);
        var userId = GetEffectiveUserId(effectivePrincipal);

        if (!await permissionService.CanAccessFormAsync(effectivePrincipal, formId, PlatformPermissions.Form.Submit, cancellationToken))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status403Forbidden, "Record API access was denied.");
        }

        var record = await recordSubmission.SubmitRecordAsync(
            formId,
            new SubmitRecordRequest(request.Values),
            userId,
            cancellationToken);
        var fieldAccess = await permissionService.GetFieldAccessAsync(effectivePrincipal, formId, cancellationToken);
        var response = ToPublicResponse(record, fieldAccess.HiddenFieldIds);
        await RecordIntegrationLogAsync(apiKeyPrincipal, IntegrationLogStatuses.Succeeded, "Record", record.Id, cancellationToken);

        return response;
    }

    private static PublicRecordResponse ToPublicResponse(FormRecordDto record, IReadOnlySet<string> hiddenFieldIds)
    {
        return new PublicRecordResponse(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            MaskValues(record.Values, hiddenFieldIds),
            record.CreatedAt,
            null);
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

    private async Task RecordIntegrationLogAsync(
        ClaimsPrincipal apiKeyPrincipal,
        string status,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        await integrationLogs.RecordAsync(
            new RecordIntegrationLogRequest(
                IntegrationLogDirections.Inbound,
                IntegrationLogTypes.Api,
                apiKeyPrincipal.FindFirstValue(IntegrationApiKeyClaims.IntegrationKey) ?? "unknown",
                status,
                "PublicRecordApi",
                SourceId: GetApiKeyId(apiKeyPrincipal),
                TargetEntityType: targetType,
                TargetEntityId: targetId,
                AttemptCount: 1,
                MaxAttempts: 1,
                IsRetryable: false,
                RequestMetadata: new Dictionary<string, object?>
                {
                    ["apiVersion"] = PublicRecordApiVersions.V1,
                    ["apiKeyId"] = GetApiKeyId(apiKeyPrincipal)
                }),
            GetEffectiveUserId(PublicRecordApiAccess.CreateEffectiveUserPrincipal(apiKeyPrincipal)),
            cancellationToken);
    }

    private static void EnsureScope(ClaimsPrincipal apiKeyPrincipal, string scope)
    {
        if (!PublicRecordApiAccess.HasScope(apiKeyPrincipal, scope))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status403Forbidden, "API key does not have the required record API scope.");
        }
    }

    private static Guid? GetApiKeyId(ClaimsPrincipal apiKeyPrincipal)
    {
        var value = apiKeyPrincipal.FindFirstValue(IntegrationApiKeyClaims.ApiKeyId);
        return Guid.TryParse(value, out var apiKeyId) ? apiKeyId : null;
    }

    private static Guid? GetEffectiveUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
