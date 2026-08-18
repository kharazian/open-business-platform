using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public sealed class ReportManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly RecordLookupService recordLookup;
    private readonly ReportRelationshipFieldService relationshipFields;

    public ReportManagementService(
        OpenBusinessPlatformDbContext dbContext,
        RecordLookupService recordLookup,
        ReportRelationshipFieldService relationshipFields)
    {
        this.dbContext = dbContext;
        this.recordLookup = recordLookup;
        this.relationshipFields = relationshipFields;
    }

    public async Task<ReportFieldCatalogDto> ListReportFieldsAsync(
        ClaimsPrincipal principal,
        Guid formId,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms.AsNoTracking().Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);
        if (form is null) throw new ReportManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        var schema = ResolveReportSchema(form);
        if (schema is null) throw new ReportManagementException(StatusCodes.Status409Conflict, "Form schema is not available for report building.");
        var structural = await relationshipFields.BuildStructuralCatalogAsync(formId, schema, cancellationToken);
        var permitted = await relationshipFields.FilterPermittedAsync(
            principal, formId, schema, structural, PlatformPermissions.Form.View, permissionService, cancellationToken);
        return new ReportFieldCatalogDto(permitted.Fields.Values.OrderBy(field => field.Source == ReportableFieldSources.Relationship).ThenBy(field => field.Label).ToArray());
    }

    public async Task<IReadOnlyCollection<ListReportSummaryDto>> ListReportsAsync(Guid formId, CancellationToken cancellationToken)
    {
        var reports = await LoadReportSummariesAsync(formId, cancellationToken);
        return reports.Select(ToSummaryDto).ToArray();
    }

    public async Task<IReadOnlyCollection<ListReportSummaryDto>> ListAccessibleReportsAsync(
        ClaimsPrincipal principal,
        Guid formId,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var reports = await LoadReportSummariesAsync(formId, cancellationToken);
        var visibleReports = new List<ReportDefinition>();

        foreach (var report in reports)
        {
            if (await CanSeeReportInListAsync(principal, permissionService, report.Id, cancellationToken))
            {
                visibleReports.Add(report);
            }
        }

        return visibleReports.Select(ToSummaryDto).ToArray();
    }

    private async Task<ReportDefinition[]> LoadReportSummariesAsync(Guid formId, CancellationToken cancellationToken)
    {
        var formExists = await dbContext.Forms
            .AsNoTracking()
            .AnyAsync(form => form.Id == formId && !form.IsDeleted, cancellationToken);

        if (!formExists)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        return await dbContext.Reports
            .AsNoTracking()
            .Include(report => report.Form)
            .Where(report => report.FormId == formId && report.Type == ReportTypes.List && !report.IsDeleted)
            .OrderByDescending(report => report.UpdatedAt ?? report.CreatedAt)
            .ThenBy(report => report.Name)
            .ToArrayAsync(cancellationToken);
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

        var catalog = await relationshipFields.BuildStructuralCatalogAsync(formId, schema, cancellationToken);
        var validation = CombineValidation(
            ListReportConfigValidator.Validate(catalog.Fields, request.Config),
            request.Config is null ? Array.Empty<ReportValidationError>() : relationshipFields.ValidatePaths(formId, schema, request.Config, catalog));

        if (!validation.Valid)
        {
            throw new ReportManagementException(StatusCodes.Status400BadRequest, "Report config is invalid.", validation.Errors);
        }

        var normalizedConfig = NormalizeConfig(request.Config!);

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

    public async Task<ListReportDetailDto> GetListReportAsync(
        Guid formId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Include(candidate => candidate.Form)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == reportId
                && candidate.FormId == formId
                && candidate.Type == ReportTypes.List
                && !candidate.IsDeleted
                && candidate.Form != null
                && !candidate.Form.IsDeleted,
                cancellationToken);

        if (report is null)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Report was not found.");
        }

        return ToDetailDto(report);
    }

    public async Task<ListReportDetailDto> UpdateListReportAsync(
        Guid formId,
        Guid reportId,
        UpdateListReportRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
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

        EnsureConcurrencyStamp(report.ConcurrencyStamp, request.ConcurrencyStamp);

        var name = (request.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ReportManagementException(StatusCodes.Status400BadRequest, "Report name is required.");
        }

        var schema = ResolveReportSchema(report.Form);

        if (schema is null)
        {
            throw new ReportManagementException(StatusCodes.Status409Conflict, "Form schema is not available for report building.");
        }

        var catalog = await relationshipFields.BuildStructuralCatalogAsync(formId, schema, cancellationToken);
        var validation = CombineValidation(
            ListReportConfigValidator.Validate(catalog.Fields, request.Config),
            request.Config is null ? Array.Empty<ReportValidationError>() : relationshipFields.ValidatePaths(formId, schema, request.Config, catalog));

        if (!validation.Valid)
        {
            throw new ReportManagementException(StatusCodes.Status400BadRequest, "Report config is invalid.", validation.Errors);
        }

        report.Name = name;
        report.ConfigJson = SerializeConfig(NormalizeConfig(request.Config!));
        report.UpdatedById = updatedById;
        AddAudit("Report", report.Id, "report_updated", updatedById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(report);
    }

    public async Task DeleteListReportAsync(
        Guid formId,
        Guid reportId,
        Guid? deletedById,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == reportId
                && candidate.FormId == formId
                && candidate.Type == ReportTypes.List
                && !candidate.IsDeleted,
                cancellationToken);

        if (report is null)
        {
            throw new ReportManagementException(StatusCodes.Status404NotFound, "Report was not found.");
        }

        report.DeletedById = deletedById;
        dbContext.Reports.Remove(report);
        AddAudit("Report", report.Id, "report_deleted", deletedById);
        await dbContext.SaveChangesAsync(cancellationToken);
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
            request,
            executionContext.DisplayValuesByRecordId,
            executionContext.FieldsById,
            executionContext.ResolvedValuesByRecordId);
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
        return await ExportListReportCsvAsync(
            principal,
            formId,
            reportId,
            new RunListReportRequest(Search: search),
            exportedById,
            permissionService,
            cancellationToken);
    }

    public async Task<ListReportCsvExportDto> ExportListReportCsvAsync(
        ClaimsPrincipal principal,
        Guid formId,
        Guid reportId,
        RunListReportRequest request,
        Guid? exportedById,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var report = await ExportListReportDataAsync(principal, formId, reportId, request, permissionService, cancellationToken);

        AddAudit("Report", reportId, "report_exported", exportedById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ListReportCsvExporter.Export(report);
    }

    public async Task<ListReportExecutionDto> ExportListReportDataAsync(
        ClaimsPrincipal principal,
        Guid formId,
        Guid reportId,
        string? search,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        return await ExportListReportDataAsync(
            principal,
            formId,
            reportId,
            new RunListReportRequest(Search: search),
            permissionService,
            cancellationToken);
    }

    public async Task<ListReportExecutionDto> ExportListReportDataAsync(
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
            GetRecordAccessActionForReportOperation(isCsvExport: true),
            permissionService,
            cancellationToken);

        return ListReportExecutionEngine.ExecuteAll(
            executionContext.Report.Id,
            executionContext.Report.FormId,
            executionContext.Report.Name,
            executionContext.Report.Form!.Name,
            executionContext.Config,
            executionContext.Schema,
            executionContext.Records,
            request,
            executionContext.DisplayValuesByRecordId,
            executionContext.FieldsById,
            executionContext.ResolvedValuesByRecordId);
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
        var structuralCatalog = await relationshipFields.BuildStructuralCatalogAsync(formId, schema, cancellationToken);
        var validation = CombineValidation(
            ListReportConfigValidator.Validate(structuralCatalog.Fields, config),
            relationshipFields.ValidatePaths(formId, schema, config, structuralCatalog));

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
        var visibleSchema = RemoveHiddenFieldsFromSchema(schema, fieldAccess.HiddenFieldIds);
        var permittedCatalog = await relationshipFields.FilterPermittedAsync(
            principal, formId, schema, structuralCatalog, recordAction, permissionService, cancellationToken);
        var permittedConfig = FilterConfigToFields(config, permittedCatalog.Fields.Keys.ToHashSet(StringComparer.Ordinal));
        var displayValuesByRecordId = await recordLookup.ResolveLookupDisplayValuesByRecordIdAsync(
            principal,
            visibleSchema,
            records,
            permissionService,
            cancellationToken);
        var requestedFieldIds = permittedConfig.Columns.Select(column => column.FieldId)
            .Concat(permittedConfig.Filters.Select(filter => filter.FieldId))
            .Concat(permittedConfig.Sort.Select(sort => sort.FieldId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var resolvedValuesByRecordId = await relationshipFields.ResolveAsync(
            principal, records, requestedFieldIds, permittedCatalog, recordAction, permissionService, cancellationToken);

        return new ListReportExecutionContext(
            report,
            visibleSchema,
            permittedConfig,
            records,
            displayValuesByRecordId,
            permittedCatalog.Fields,
            resolvedValuesByRecordId);
    }

    private static string GetRecordAccessActionForReportOperation(bool isCsvExport)
    {
        return isCsvExport ? PlatformPermissions.Form.Export : PlatformPermissions.Form.View;
    }

    private static async Task<bool> CanSeeReportInListAsync(
        ClaimsPrincipal principal,
        PermissionService permissionService,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        foreach (var action in PlatformPermissions.ReportActions)
        {
            if (await permissionService.CanAccessReportAsync(principal, reportId, action, cancellationToken))
            {
                return true;
            }
        }

        return false;
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
                .ToArray(),
            NormalizeRowOpenAction(config.RowOpenAction));
    }

    private static ListReportConfigDefinition FilterConfigToFields(
        ListReportConfigDefinition config,
        IReadOnlySet<string> allowedFieldIds)
    {
        return config with
        {
            Columns = config.Columns.Where(column => allowedFieldIds.Contains(column.FieldId)).ToArray(),
            Filters = config.Filters.Where(filter => allowedFieldIds.Contains(filter.FieldId)).ToArray(),
            Sort = config.Sort.Where(sort => allowedFieldIds.Contains(sort.FieldId)).ToArray()
        };
    }

    private static ReportValidationResult CombineValidation(ReportValidationResult validation, IReadOnlyList<ReportValidationError> pathErrors)
    {
        return pathErrors.Count == 0 ? validation : new ReportValidationResult(validation.Errors.Concat(pathErrors).ToArray());
    }

    private static FormSchemaDefinition RemoveHiddenFieldsFromSchema(
        FormSchemaDefinition schema,
        IReadOnlySet<string> hiddenFieldIds)
    {
        if (hiddenFieldIds.Count == 0)
        {
            return schema;
        }

        return schema with
        {
            Fields = schema.Fields
                .Where(field => !hiddenFieldIds.Contains(field.Id))
                .ToArray(),
            Layout = RemoveHiddenFieldsFromLayout(schema.Layout, hiddenFieldIds)
        };
    }

    private static FormLayoutDefinition RemoveHiddenFieldsFromLayout(
        FormLayoutDefinition layout,
        IReadOnlySet<string> hiddenFieldIds)
    {
        return layout with
        {
            Pages = layout.Pages.Select(page => page with
            {
                Sections = page.Sections.Select(section => section with
                {
                    Rows = section.Rows.Select(row => row with
                    {
                        Columns = row.Columns.Select(column => column with
                        {
                            Fields = column.Fields
                                .Where(fieldId => !hiddenFieldIds.Contains(fieldId))
                                .ToArray()
                        }).ToArray()
                    }).ToArray()
                }).ToArray()
            }).ToArray()
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeRowOpenAction(string? value)
    {
        var normalized = value?.Trim();
        return normalized is not null && ListReportRowOpenActions.Supported.Contains(normalized)
            ? normalized
            : ListReportRowOpenActions.Detail;
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string? requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new ReportManagementException(StatusCodes.Status409Conflict, "Report was updated by someone else. Refresh and try again.");
        }
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
        IReadOnlyCollection<FormRecord> Records,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> DisplayValuesByRecordId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> FieldsById,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>> ResolvedValuesByRecordId);
}
