using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public sealed class ChartAggregationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public ChartAggregationService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ChartWidgetPreviewDto> PreviewAsync(
        ClaimsPrincipal principal,
        Guid formId,
        ChartWidgetConfigDefinition request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new ChartAggregationException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var schema = DeserializeSchema(form.CurrentVersion?.SchemaJson) ?? DeserializeSchema(form.DraftSchemaJson);

        if (schema is null)
        {
            throw new ChartAggregationException(StatusCodes.Status409Conflict, "Form schema is not available for chart rendering.");
        }

        var validation = ChartWidgetConfigValidator.Validate(schema, request);

        if (!validation.Valid)
        {
            throw new ChartAggregationException(StatusCodes.Status400BadRequest, "Chart config is invalid.", validation.Errors);
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, formId, cancellationToken);
        var sanitizedRequest = EnsureVisibleConfig(request, fieldAccess.HiddenFieldIds);
        var sourceReportConfig = await GetSourceReportConfigAsync(
            principal,
            permissionService,
            formId,
            request.ReportId,
            schema,
            fieldAccess.HiddenFieldIds,
            cancellationToken);
        var scopedRecordsQuery = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == formId && !record.IsDeleted && record.Status != RecordStatuses.Deleted),
            formId,
            PlatformPermissions.Form.View,
            cancellationToken);
        var records = await scopedRecordsQuery
            .ToArrayAsync(cancellationToken);

        return ChartAggregationEngine.Execute(form.Id, form.Name, sanitizedRequest, schema, records, sourceReportConfig, fieldAccess.HiddenFieldIds);
    }

    private async Task<ListReportConfigDefinition?> GetSourceReportConfigAsync(
        ClaimsPrincipal principal,
        PermissionService permissionService,
        Guid formId,
        Guid? reportId,
        FormSchemaDefinition schema,
        IReadOnlySet<string> hiddenFieldIds,
        CancellationToken cancellationToken)
    {
        if (reportId is null)
        {
            return null;
        }

        if (!await permissionService.CanAccessReportAsync(principal, reportId.Value, PlatformPermissions.Report.View, cancellationToken))
        {
            throw new ChartAggregationException(StatusCodes.Status403Forbidden, "Source report access was denied.");
        }

        var report = await dbContext.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == reportId
                && candidate.FormId == formId
                && candidate.Type == ReportTypes.List
                && !candidate.IsDeleted,
                cancellationToken);

        if (report is null)
        {
            throw new ChartAggregationException(StatusCodes.Status404NotFound, "Source report was not found.");
        }

        var config = DeserializeReportConfig(report.ConfigJson);
        var validation = ListReportConfigValidator.Validate(schema, config);

        if (!validation.Valid)
        {
            throw new ChartAggregationException(StatusCodes.Status409Conflict, "Source report config no longer matches the form schema.");
        }

        return RemoveHiddenFields(config, hiddenFieldIds);
    }

    private static ChartWidgetConfigDefinition EnsureVisibleConfig(
        ChartWidgetConfigDefinition config,
        IReadOnlySet<string> hiddenFieldIds)
    {
        if (hiddenFieldIds.Count == 0)
        {
            return config;
        }

        var hiddenMetric = config.Metric.FieldId is not null && hiddenFieldIds.Contains(config.Metric.FieldId);
        var hiddenGroup = config.GroupByFieldId is not null && hiddenFieldIds.Contains(config.GroupByFieldId);
        var hiddenDate = config.DateFieldId is not null && hiddenFieldIds.Contains(config.DateFieldId);

        if (hiddenMetric || hiddenGroup || hiddenDate)
        {
            throw new ChartAggregationException(StatusCodes.Status403Forbidden, "Chart config references a hidden field.");
        }

        return config with
        {
            Columns = (config.Columns ?? Array.Empty<string>())
                .Where(column => !hiddenFieldIds.Contains(column))
                .ToArray()
        };
    }

    private static ListReportConfigDefinition RemoveHiddenFields(
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

    private static ListReportConfigDefinition DeserializeReportConfig(JsonDocument configJson)
    {
        return configJson.RootElement.Deserialize<ListReportConfigDefinition>(JsonOptions)
            ?? new ListReportConfigDefinition(1, Array.Empty<ListReportColumnDefinition>(), Array.Empty<ListReportFilterDefinition>(), Array.Empty<ListReportSortDefinition>());
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }
}
