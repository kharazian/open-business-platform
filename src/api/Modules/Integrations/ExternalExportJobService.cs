using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class ExternalExportJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly PermissionService permissionService;
    private readonly ReportManagementService reportManagement;
    private readonly IntegrationLogService integrationLogs;

    public ExternalExportJobService(
        OpenBusinessPlatformDbContext dbContext,
        PermissionService permissionService,
        ReportManagementService reportManagement,
        IntegrationLogService integrationLogs)
    {
        this.dbContext = dbContext;
        this.permissionService = permissionService;
        this.reportManagement = reportManagement;
        this.integrationLogs = integrationLogs;
    }

    public async Task<IReadOnlyCollection<ExternalExportJobSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var jobs = await dbContext.ExternalExportJobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return jobs.Select(ToSummaryDto).ToArray();
    }

    public async Task<ExternalExportJobDetailDto?> GetAsync(Guid exportJobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.ExternalExportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == exportJobId, cancellationToken);

        return job is null ? null : ToDetailDto(job);
    }

    public async Task<ExternalExportJobDetailDto> CreateAsync(
        ClaimsPrincipal principal,
        CreateExternalExportJobRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        var startedAt = DateTimeOffset.UtcNow;
        var job = new ExternalExportJob
        {
            Id = Guid.NewGuid(),
            SourceType = normalized.SourceType,
            Format = normalized.Format,
            IntegrationKey = normalized.IntegrationKey,
            FormId = normalized.FormId,
            ReportId = normalized.ReportId,
            Status = ExternalExportJobStatuses.Running,
            StartedAt = startedAt,
            RequestJson = JsonSerializer.SerializeToDocument(normalized, JsonOptions),
            CreatedById = createdById
        };

        dbContext.ExternalExportJobs.Add(job);
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "ExternalExportJob",
            EntityId = job.Id,
            Action = "external_export_job_started",
            UserId = createdById,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                job.SourceType,
                job.Format,
                job.FormId,
                job.ReportId,
                job.IntegrationKey
            }, JsonOptions)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var report = string.Equals(normalized.SourceType, ExternalExportJobSourceTypes.ListReport, StringComparison.Ordinal)
                ? await BuildReportExportAsync(principal, normalized, cancellationToken)
                : await BuildFormRecordsExportAsync(principal, normalized, cancellationToken);
            var artifact = ExternalExportArtifactBuilder.Build(normalized.Format, report);
            job.Status = ExternalExportJobStatuses.Succeeded;
            job.RowCount = report.Rows.Count;
            job.ArtifactFileName = artifact.FileName;
            job.ArtifactContentType = artifact.ContentType;
            job.ArtifactSizeBytes = artifact.SizeBytes;
            job.ArtifactContent = artifact.Content;
            job.ArtifactMetadataJson = JsonSerializer.SerializeToDocument(new
            {
                artifact.FileName,
                artifact.ContentType,
                artifact.SizeBytes,
                report.TotalCount,
                columnCount = report.Columns.Count
            }, JsonOptions);
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.UpdatedById = createdById;

            await dbContext.SaveChangesAsync(cancellationToken);
            await RecordIntegrationLogAsync(job, createdById, null, null, cancellationToken);
            return ToDetailDto(job);
        }
        catch (Exception exception) when (exception is ExternalExportException or ReportManagementException)
        {
            job.Status = ExternalExportJobStatuses.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.UpdatedById = createdById;
            await dbContext.SaveChangesAsync(cancellationToken);
            await RecordIntegrationLogAsync(job, createdById, "external_export_failed", exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task<ListReportExecutionDto> BuildReportExportAsync(
        ClaimsPrincipal principal,
        CreateExternalExportJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FormId is null || request.ReportId is null)
        {
            throw new ExternalExportException(StatusCodes.Status400BadRequest, "Report exports require formId and reportId.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, request.FormId.Value, PlatformPermissions.Form.Export, cancellationToken))
        {
            throw new ExternalExportException(StatusCodes.Status403Forbidden, "Form export access was denied.");
        }

        if (!await permissionService.CanAccessReportAsync(principal, request.ReportId.Value, PlatformPermissions.Report.Export, cancellationToken))
        {
            throw new ExternalExportException(StatusCodes.Status403Forbidden, "Report export access was denied.");
        }

        return await reportManagement.ExportListReportDataAsync(
            principal,
            request.FormId.Value,
            request.ReportId.Value,
            request.Search,
            permissionService,
            cancellationToken);
    }

    private async Task<ListReportExecutionDto> BuildFormRecordsExportAsync(
        ClaimsPrincipal principal,
        CreateExternalExportJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FormId is null)
        {
            throw new ExternalExportException(StatusCodes.Status400BadRequest, "Form record exports require formId.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, request.FormId.Value, PlatformPermissions.Form.Export, cancellationToken))
        {
            throw new ExternalExportException(StatusCodes.Status403Forbidden, "Form export access was denied.");
        }

        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == request.FormId.Value && !candidate.IsDeleted, cancellationToken);
        if (form?.CurrentVersion?.SchemaJson is null)
        {
            throw new ExternalExportException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var schema = form.CurrentVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
        if (schema is null)
        {
            throw new ExternalExportException(StatusCodes.Status409Conflict, "Form schema is invalid.");
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, form.Id, cancellationToken);
        var recordsQuery = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == form.Id && !record.IsDeleted),
            form.Id,
            PlatformPermissions.Form.Export,
            cancellationToken);
        var records = await recordsQuery
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .ToArrayAsync(cancellationToken);
        var columns = schema.Fields
            .Where(field => !fieldAccess.HiddenFieldIds.Contains(field.Id))
            .Select(field => new ListReportExecutionColumnDto(field.Id, field.Label, field.Type, "field", null))
            .ToArray();
        var rows = records.Select(record =>
        {
            var values = DeserializeValues(record.ValuesJson);
            var cells = columns.ToDictionary(
                column => column.FieldId,
                column =>
                {
                    values.TryGetValue(column.FieldId, out var value);
                    return new ListReportExecutionCellDto(value, Convert.ToString(value) ?? string.Empty);
                },
                StringComparer.Ordinal);
            return new ListReportExecutionRowDto(record.Id, record.Status, cells, record.CreatedAt);
        }).ToArray();

        return new ListReportExecutionDto(
            Guid.Empty,
            form.Id,
            $"{form.Name} records",
            form.Name,
            1,
            rows.Length,
            rows.LongLength,
            columns,
            rows);
    }

    private async Task RecordIntegrationLogAsync(
        ExternalExportJob job,
        Guid? createdById,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        await integrationLogs.RecordAsync(
            new RecordIntegrationLogRequest(
                IntegrationLogDirections.Outbound,
                IntegrationLogTypes.Export,
                job.IntegrationKey,
                job.Status == ExternalExportJobStatuses.Succeeded ? IntegrationLogStatuses.Succeeded : IntegrationLogStatuses.Failed,
                "ExternalExportJob",
                SourceId: job.Id,
                TargetEntityType: job.SourceType,
                TargetEntityId: job.ReportId ?? job.FormId,
                AttemptCount: 1,
                MaxAttempts: 1,
                IsRetryable: false,
                RequestMetadata: new Dictionary<string, object?>
                {
                    ["sourceType"] = job.SourceType,
                    ["format"] = job.Format,
                    ["formId"] = job.FormId,
                    ["reportId"] = job.ReportId
                },
                ResponseMetadata: new Dictionary<string, object?>
                {
                    ["status"] = job.Status,
                    ["rowCount"] = job.RowCount,
                    ["artifactFileName"] = job.ArtifactFileName,
                    ["artifactSizeBytes"] = job.ArtifactSizeBytes
                },
                ErrorCode: errorCode,
                ErrorMessage: errorMessage,
                StartedAt: job.StartedAt,
                CompletedAt: job.CompletedAt),
            createdById,
            cancellationToken);
    }

    private static CreateExternalExportJobRequest Normalize(CreateExternalExportJobRequest request)
    {
        var normalized = request with
        {
            SourceType = request.SourceType.Trim().ToLowerInvariant(),
            Format = request.Format.Trim().ToLowerInvariant(),
            IntegrationKey = request.IntegrationKey.Trim().ToLowerInvariant(),
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()
        };

        if (!ExternalExportJobSourceTypes.Supported.Contains(normalized.SourceType))
        {
            throw new ExternalExportException(StatusCodes.Status400BadRequest, "Export source type is invalid.");
        }

        if (!ExternalExportJobFormats.Supported.Contains(normalized.Format))
        {
            throw new ExternalExportException(StatusCodes.Status400BadRequest, "Export format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(normalized.IntegrationKey))
        {
            throw new ExternalExportException(StatusCodes.Status400BadRequest, "Integration key is required.");
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static ExternalExportJobSummaryDto ToSummaryDto(ExternalExportJob job)
    {
        return new ExternalExportJobSummaryDto(
            job.Id,
            job.SourceType,
            job.Format,
            job.IntegrationKey,
            job.FormId,
            job.ReportId,
            job.Status,
            job.RowCount,
            job.ArtifactFileName,
            job.ArtifactContentType,
            job.ArtifactSizeBytes,
            job.StartedAt,
            job.CompletedAt,
            job.CreatedAt,
            job.CreatedById);
    }

    private static ExternalExportJobDetailDto ToDetailDto(ExternalExportJob job)
    {
        return new ExternalExportJobDetailDto(
            job.Id,
            job.SourceType,
            job.Format,
            job.IntegrationKey,
            job.FormId,
            job.ReportId,
            job.Status,
            job.RowCount,
            job.ArtifactFileName,
            job.ArtifactContentType,
            job.ArtifactSizeBytes,
            job.ArtifactContent,
            job.ArtifactMetadataJson?.RootElement.Deserialize<Dictionary<string, object?>>(JsonOptions),
            job.StartedAt,
            job.CompletedAt,
            job.ConcurrencyStamp,
            job.CreatedAt,
            job.CreatedById,
            job.UpdatedAt,
            job.UpdatedById);
    }
}
