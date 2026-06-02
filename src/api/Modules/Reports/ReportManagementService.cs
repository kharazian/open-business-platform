using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public sealed class ReportManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public ReportManagementService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ListReportSummaryDto>> ListReportsAsync(Guid formId, CancellationToken cancellationToken)
    {
        var formExists = await dbContext.Forms
            .AsNoTracking()
            .AnyAsync(form => form.Id == formId && !form.IsDeleted, cancellationToken);

        if (!formExists)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var reports = await dbContext.Reports
            .AsNoTracking()
            .Include(report => report.Form)
            .Where(report => report.FormId == formId && report.Type == ReportTypes.List && !report.IsDeleted)
            .OrderByDescending(report => report.UpdatedAt ?? report.CreatedAt)
            .ThenBy(report => report.Name)
            .ToArrayAsync(cancellationToken);

        return reports.Select(ToSummaryDto).ToArray();
    }

    public async Task<ListReportDetailDto> CreateListReportAsync(
        Guid formId,
        CreateListReportRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var name = (request.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ReportManagementException(StatusCodes.Status400BadRequest, "Report name is required.");
        }

        var schema = ResolveReportSchema(form);

        if (schema is null)
        {
            throw new ReportManagementException(StatusCodes.Status409Conflict, "Form schema is not available for report building.");
        }

        var validation = ListReportConfigValidator.Validate(schema, request.Config);

        if (!validation.Valid)
        {
            throw new ReportManagementException(StatusCodes.Status400BadRequest, "Report config is invalid.", validation.Errors);
        }

        var normalizedConfig = NormalizeConfig(request.Config);

        var report = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Name = name,
            Type = ReportTypes.List,
            ConfigJson = SerializeConfig(normalizedConfig),
            CreatedById = createdById
        };

        dbContext.Reports.Add(report);
        AddAudit("Report", report.Id, "report_created", createdById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(report);
    }

    public async Task<ListReportExecutionDto> ExecuteListReportAsync(
        ClaimsPrincipal principal,
        Guid formId,
        Guid reportId,
        RunListReportRequest request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var executionContext = await LoadReportExecutionContextAsync(
            principal,
            formId,
            reportId,
            GetRecordAccessActionForReportOperation(isCsvExport: false),
            permissionService,
            cancellationToken);

        return ListReportExecutionEngine.Execute(
            executionContext.Report.Id,
            executionContext.Report.FormId,
            executionContext.Report.Name,
            executionContext.Report.Form!.Name,
            executionContext.Config,
            executionContext.Schema,
            executionContext.Records,
            request);
    }

    public async Task<ListReportCsvExportDto> ExportListReportCsvAsync(
        ClaimsPrincipal principal,
        Guid formId,
        Guid reportId,
        string? search,
        Guid? exportedById,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var executionContext = await LoadReportExecutionContextAsync(
            principal,
            formId,
            reportId,
            GetRecordAccessActionForReportOperation(isCsvExport: true),
            permissionService,
            cancellationToken);
        var report = ListReportExecutionEngine.ExecuteAll(
            executionContext.Report.Id,
            executionContext.Report.FormId,
            executionContext.Report.Name,
            executionContext.Report.Form!.Name,
            executionContext.Config,
            executionContext.Schema,
            executionContext.Records,
            search);

        AddAudit("Report", reportId, "report_exported", exportedById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ListReportCsvExporter.Export(report);
    }

    private async Task<ListReportExecutionContext> LoadReportExecutionContextAsync(
        ClaimsPrincipal principal,
        Guid formId,
        Guid reportId,
        string recordAction,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Include(candidate => candidate.Form)
            .ThenInclude(form => form!.CurrentVersion)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == reportId
                && candidate.FormId == formId
                && candidate.Type == ReportTypes.List
                && !candidate.IsDeleted
                && candidate.Form != null
                && !candidate.Form.IsDeleted,
                cancellationToken);

        if (report is null || report.Form is null)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Report was not found.");
        }

        var schema = ResolveExecutionSchema(report.Form);

        if (schema is null)
        {
            throw new ReportManagementException(StatusCodes.Status409Conflict, "Form schema is not available for report running.");
        }

        var config = DeserializeConfig(report.ConfigJson);
        var validation = ListReportConfigValidator.Validate(schema, config);

        if (!validation.Valid)
        {
            throw new ReportManagementException(StatusCodes.Status409Conflict, "Report config no longer matches the form schema.", validation.Errors);
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, formId, cancellationToken);
        var scopedRecordsQuery = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == formId && !record.IsDeleted),
            formId,
            recordAction,
            cancellationToken);
        var records = await scopedRecordsQuery
            .ToArrayAsync(cancellationToken);

        return new ListReportExecutionContext(report, schema, RemoveHiddenColumns(config, fieldAccess.HiddenFieldIds), records);
    }

    private static string GetRecordAccessActionForReportOperation(bool isCsvExport)
    {
        return isCsvExport ? PlatformPermissions.Form.Export : PlatformPermissions.Form.View;
    }

    private static ListReportSummaryDto ToSummaryDto(ReportDefinition report)
    {
        var config = DeserializeConfig(report.ConfigJson);

        return new ListReportSummaryDto(
            report.Id,
            report.FormId,
            report.Form?.Name ?? "Unknown form",
            report.Name,
            report.Type,
            config.Columns.Count(column => column.Visible),
            config.Filters.Count,
            config.Sort.Count,
            report.ConcurrencyStamp,
            report.CreatedAt,
            report.CreatedById,
            report.UpdatedAt,
            report.UpdatedById);
    }

    private static ListReportDetailDto ToDetailDto(ReportDefinition report)
    {
        return new ListReportDetailDto(
            report.Id,
            report.FormId,
            report.Form?.Name ?? "Unknown form",
            report.Name,
            report.Type,
            DeserializeConfig(report.ConfigJson),
            report.ConcurrencyStamp,
            report.CreatedAt,
            report.CreatedById,
            report.UpdatedAt,
            report.UpdatedById);
    }

    private void AddAudit(string entityType, Guid entityId, string action, Guid? userId = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId
        });
    }

    private static ListReportConfigDefinition NormalizeConfig(ListReportConfigDefinition config)
    {
        return new ListReportConfigDefinition(
            config.SchemaVersion,
            (config.Columns ?? Array.Empty<ListReportColumnDefinition>())
                .Select(column => new ListReportColumnDefinition(
                    column.FieldId.Trim(),
                    column.Label.Trim(),
                    column.Visible,
                    column.Width))
                .ToArray(),
            (config.Filters ?? Array.Empty<ListReportFilterDefinition>())
                .Select(filter => new ListReportFilterDefinition(
                    filter.FieldId.Trim(),
                    filter.Operator.Trim(),
                    NormalizeOptionalText(filter.Value)))
                .ToArray(),
            (config.Sort ?? Array.Empty<ListReportSortDefinition>())
                .Select(sort => new ListReportSortDefinition(sort.FieldId.Trim(), sort.Direction.Trim()))
                .ToArray());
    }

    private static ListReportConfigDefinition RemoveHiddenColumns(
        ListReportConfigDefinition config,
        IReadOnlySet<string> hiddenFieldIds)
    {
        if (hiddenFieldIds.Count == 0)
        {
            return config;
        }

        return config with
        {
            Columns = config.Columns.Where(column => !hiddenFieldIds.Contains(column.FieldId)).ToArray(),
            Filters = config.Filters.Where(filter => !hiddenFieldIds.Contains(filter.FieldId)).ToArray(),
            Sort = config.Sort.Where(sort => !hiddenFieldIds.Contains(sort.FieldId)).ToArray()
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static FormSchemaDefinition? ResolveReportSchema(FormDefinition form)
    {
        return DeserializeSchema(form.DraftSchemaJson) ?? DeserializeSchema(form.CurrentVersion?.SchemaJson);
    }

    private static FormSchemaDefinition? ResolveExecutionSchema(FormDefinition form)
    {
        return DeserializeSchema(form.CurrentVersion?.SchemaJson) ?? DeserializeSchema(form.DraftSchemaJson);
    }

    private static JsonDocument SerializeConfig(ListReportConfigDefinition config)
    {
        return JsonSerializer.SerializeToDocument(config, JsonOptions);
    }

    private static ListReportConfigDefinition DeserializeConfig(JsonDocument configJson)
    {
        return configJson.RootElement.Deserialize<ListReportConfigDefinition>(JsonOptions)
            ?? new ListReportConfigDefinition(1, Array.Empty<ListReportColumnDefinition>(), Array.Empty<ListReportFilterDefinition>(), Array.Empty<ListReportSortDefinition>());
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }

    private sealed record ListReportExecutionContext(
        ReportDefinition Report,
        FormSchemaDefinition Schema,
        ListReportConfigDefinition Config,
        IReadOnlyCollection<FormRecord> Records);
}
