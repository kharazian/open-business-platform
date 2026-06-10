using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class RecordImportJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly RecordSubmissionService recordSubmission;
    private readonly PermissionService permissionService;
    private readonly IntegrationLogService integrationLogs;

    public RecordImportJobService(
        OpenBusinessPlatformDbContext dbContext,
        RecordSubmissionService recordSubmission,
        PermissionService permissionService,
        IntegrationLogService integrationLogs)
    {
        this.dbContext = dbContext;
        this.recordSubmission = recordSubmission;
        this.permissionService = permissionService;
        this.integrationLogs = integrationLogs;
    }

    public async Task<IReadOnlyCollection<RecordImportJobSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var jobs = await dbContext.RecordImportJobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return jobs.Select(ToSummaryDto).ToArray();
    }

    public async Task<RecordImportJobDetailDto?> GetAsync(Guid importJobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.RecordImportJobs
            .AsNoTracking()
            .Include(candidate => candidate.Rows.OrderBy(row => row.RowNumber))
            .SingleOrDefaultAsync(candidate => candidate.Id == importJobId, cancellationToken);

        return job is null ? null : ToDetailDto(job);
    }

    public async Task<RecordImportJobDetailDto> CreateAsync(
        ClaimsPrincipal principal,
        CreateRecordImportJobRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == request.FormId && !candidate.IsDeleted, cancellationToken);

        if (form?.CurrentVersion?.SchemaJson is null)
        {
            throw new RecordImportException(StatusCodes.Status404NotFound, "Target form was not found.");
        }

        if (!string.Equals(form.Status, FormStatuses.Published, StringComparison.Ordinal)
            || form.CurrentVersionId is null)
        {
            throw new RecordImportException(StatusCodes.Status409Conflict, "Record imports require a published target form.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, request.FormId, PlatformPermissions.Form.Submit, cancellationToken))
        {
            throw new RecordImportException(StatusCodes.Status403Forbidden, "Record import access was denied.");
        }

        var schema = form.CurrentVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
        if (schema is null)
        {
            throw new RecordImportException(StatusCodes.Status409Conflict, "Target form schema is invalid.");
        }

        var csv = RecordImportCsvParser.Parse(request.CsvContent);
        var normalized = Normalize(request);
        var validation = RecordImportJobValidator.Validate(normalized, schema, csv);
        if (!validation.Valid)
        {
            throw new RecordImportException(StatusCodes.Status400BadRequest, "Record import job is invalid.");
        }

        var now = DateTimeOffset.UtcNow;
        var job = new RecordImportJob
        {
            Id = Guid.NewGuid(),
            FormId = normalized.FormId,
            IntegrationKey = normalized.IntegrationKey,
            FileName = NormalizeFileName(normalized.FileName),
            Status = RecordImportJobStatuses.Running,
            TotalRows = csv.Rows.Count,
            StartedAt = now,
            MappingJson = JsonSerializer.SerializeToDocument(normalized.Mapping, JsonOptions),
            CreatedById = createdById
        };

        dbContext.RecordImportJobs.Add(job);
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "RecordImportJob",
            EntityId = job.Id,
            Action = "record_import_job_started",
            UserId = createdById,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                job.FormId,
                job.IntegrationKey,
                job.FileName,
                job.TotalRows
            }, JsonOptions)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var row in csv.Rows)
        {
            await ProcessRowAsync(job, normalized.Mapping, row, createdById, cancellationToken);
        }

        job.SucceededRows = await dbContext.RecordImportJobRows.CountAsync(
            row => row.ImportJobId == job.Id && row.Status == RecordImportJobRowStatuses.Succeeded,
            cancellationToken);
        job.FailedRows = await dbContext.RecordImportJobRows.CountAsync(
            row => row.ImportJobId == job.Id && row.Status == RecordImportJobRowStatuses.Failed,
            cancellationToken);
        job.Status = job.FailedRows == 0
            ? RecordImportJobStatuses.Succeeded
            : job.SucceededRows == 0
                ? RecordImportJobStatuses.Failed
                : RecordImportJobStatuses.CompletedWithErrors;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.UpdatedById = createdById;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordIntegrationLogAsync(job, createdById, cancellationToken);

        return (await GetAsync(job.Id, cancellationToken))!;
    }

    private async Task ProcessRowAsync(
        RecordImportJob job,
        RecordImportMappingDefinition mapping,
        RecordImportCsvRow row,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        try
        {
            var values = MapValues(mapping, row);
            var record = await recordSubmission.SubmitRecordAsync(
                job.FormId,
                new SubmitRecordRequest(values),
                createdById,
                cancellationToken);

            dbContext.RecordImportJobRows.Add(new RecordImportJobRow
            {
                Id = Guid.NewGuid(),
                ImportJobId = job.Id,
                RowNumber = row.RowNumber,
                Status = RecordImportJobRowStatuses.Succeeded,
                RecordId = record.Id
            });
        }
        catch (RecordSubmissionException exception)
        {
            dbContext.RecordImportJobRows.Add(new RecordImportJobRow
            {
                Id = Guid.NewGuid(),
                ImportJobId = job.Id,
                RowNumber = row.RowNumber,
                Status = RecordImportJobRowStatuses.Failed,
                ErrorsJson = JsonSerializer.SerializeToDocument(exception.Errors, JsonOptions)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordIntegrationLogAsync(
        RecordImportJob job,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        await integrationLogs.RecordAsync(
            new RecordIntegrationLogRequest(
                IntegrationLogDirections.Inbound,
                IntegrationLogTypes.Import,
                job.IntegrationKey,
                job.FailedRows == 0 ? IntegrationLogStatuses.Succeeded : IntegrationLogStatuses.Failed,
                "RecordImportJob",
                SourceId: job.Id,
                TargetEntityType: "Form",
                TargetEntityId: job.FormId,
                AttemptCount: 1,
                MaxAttempts: 1,
                IsRetryable: false,
                RequestMetadata: new Dictionary<string, object?>
                {
                    ["fileName"] = job.FileName,
                    ["totalRows"] = job.TotalRows,
                    ["mappedFieldCount"] = DeserializeMapping(job.MappingJson).FieldMappings.Count
                },
                ResponseMetadata: new Dictionary<string, object?>
                {
                    ["status"] = job.Status,
                    ["succeededRows"] = job.SucceededRows,
                    ["failedRows"] = job.FailedRows
                },
                ErrorCode: job.FailedRows == 0 ? null : "record_import_rows_failed",
                ErrorMessage: job.FailedRows == 0 ? null : "One or more import rows failed validation.",
                StartedAt: job.StartedAt,
                CompletedAt: job.CompletedAt),
            createdById,
            cancellationToken);
    }

    private static IReadOnlyDictionary<string, object?> MapValues(
        RecordImportMappingDefinition mapping,
        RecordImportCsvRow row)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var fieldMapping in mapping.FieldMappings)
        {
            values[fieldMapping.TargetFieldId] = row.Values.TryGetValue(fieldMapping.CsvHeader, out var value)
                ? string.IsNullOrWhiteSpace(value) ? null : value
                : null;
        }

        return values;
    }

    private static CreateRecordImportJobRequest Normalize(CreateRecordImportJobRequest request)
    {
        return request with
        {
            IntegrationKey = request.IntegrationKey.Trim().ToLowerInvariant(),
            Mapping = new RecordImportMappingDefinition(request.Mapping.FieldMappings.Select(mapping => mapping with
            {
                CsvHeader = mapping.CsvHeader.Trim(),
                TargetFieldId = mapping.TargetFieldId.Trim()
            }).ToArray())
        };
    }

    private static string? NormalizeFileName(string? fileName)
    {
        var normalized = fileName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var safe = Path.GetFileName(normalized);
        return safe.Length <= 260 ? safe : safe[..260];
    }

    private static RecordImportJobSummaryDto ToSummaryDto(RecordImportJob job)
    {
        return new RecordImportJobSummaryDto(
            job.Id,
            job.FormId,
            job.IntegrationKey,
            job.FileName,
            job.Status,
            job.TotalRows,
            job.SucceededRows,
            job.FailedRows,
            job.StartedAt,
            job.CompletedAt,
            job.CreatedAt,
            job.CreatedById);
    }

    private static RecordImportJobDetailDto ToDetailDto(RecordImportJob job)
    {
        return new RecordImportJobDetailDto(
            job.Id,
            job.FormId,
            job.IntegrationKey,
            job.FileName,
            job.Status,
            job.TotalRows,
            job.SucceededRows,
            job.FailedRows,
            DeserializeMapping(job.MappingJson),
            job.Rows.OrderBy(row => row.RowNumber).Select(ToRowDto).ToArray(),
            job.StartedAt,
            job.CompletedAt,
            job.ConcurrencyStamp,
            job.CreatedAt,
            job.CreatedById,
            job.UpdatedAt,
            job.UpdatedById);
    }

    private static RecordImportJobRowDto ToRowDto(RecordImportJobRow row)
    {
        return new RecordImportJobRowDto(
            row.Id,
            row.RowNumber,
            row.Status,
            row.RecordId,
            row.ErrorsJson?.RootElement.Deserialize<IReadOnlyList<FormValidationError>>(JsonOptions)
                ?? Array.Empty<FormValidationError>());
    }

    private static RecordImportMappingDefinition DeserializeMapping(JsonDocument mappingJson)
    {
        return mappingJson.RootElement.Deserialize<RecordImportMappingDefinition>(JsonOptions)
            ?? new RecordImportMappingDefinition(Array.Empty<RecordImportFieldMappingDefinition>());
    }
}
