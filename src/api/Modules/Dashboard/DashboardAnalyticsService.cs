using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public sealed class DashboardAnalyticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public DashboardAnalyticsService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DashboardAnalyticsResponse> RunAsync(
        ClaimsPrincipal principal,
        DashboardAnalyticsRequest request,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        EnsureSourceRequest(request);
        var formId = request.Source.FormId;

        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new DashboardAnalyticsException(StatusCodes.Status404NotFound, "Source form was not found.");
        }

        if (!await permissionService.CanAccessFormAsync(principal, formId, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new DashboardAnalyticsException(StatusCodes.Status403Forbidden, "Source form access was denied.");
        }

        var schema = DeserializeSchema(form.CurrentVersion?.SchemaJson) ?? DeserializeSchema(form.DraftSchemaJson);

        if (schema is null)
        {
            throw new DashboardAnalyticsException(StatusCodes.Status409Conflict, "Source form schema is not available for dashboard analytics.");
        }

        var validation = DashboardAnalyticsRequestValidator.Validate(schema, request);

        if (!validation.Valid)
        {
            throw new DashboardAnalyticsException(StatusCodes.Status400BadRequest, "Dashboard analytics request is invalid.", validation.Errors);
        }

        var fieldAccess = await permissionService.GetFieldAccessAsync(principal, formId, cancellationToken);
        var sanitizedRequest = EnsureVisibleRequest(request, fieldAccess.HiddenFieldIds);
        var sourceReportConfig = await GetSourceReportConfigAsync(
            principal,
            permissionService,
            formId,
            sanitizedRequest.Source.ReportId,
            schema,
            fieldAccess.HiddenFieldIds,
            cancellationToken);
        var scopedRecordsQuery = await permissionService.ApplyRecordAccessAsync(
            principal,
            dbContext.Records.AsNoTracking().Where(record => record.FormId == formId && !record.IsDeleted && record.Status != RecordStatuses.Deleted),
            formId,
            PlatformPermissions.Form.View,
            cancellationToken);
        var records = await scopedRecordsQuery.ToArrayAsync(cancellationToken);
        var chartConfig = ToChartConfig(sanitizedRequest);
        var chartResult = ChartAggregationEngine.Execute(
            form.Id,
            form.Name,
            chartConfig,
            schema,
            records,
            sourceReportConfig,
            fieldAccess.HiddenFieldIds);

        return new DashboardAnalyticsResponse(
            chartResult.FormId,
            chartResult.FormName,
            sanitizedRequest.Source.ReportId,
            sanitizedRequest.WidgetType,
            sanitizedRequest.Metric,
            chartResult.Series,
            chartResult.Columns,
            chartResult.Rows,
            chartResult.TotalCount);
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
            throw new DashboardAnalyticsException(StatusCodes.Status403Forbidden, "Source report access was denied.");
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
            throw new DashboardAnalyticsException(StatusCodes.Status404NotFound, "Source report was not found.");
        }

        var config = DeserializeReportConfig(report.ConfigJson);
        var validation = ListReportConfigValidator.Validate(schema, config);

        if (!validation.Valid)
        {
            throw new DashboardAnalyticsException(StatusCodes.Status409Conflict, "Source report config no longer matches the form schema.");
        }

        return RemoveHiddenFields(config, hiddenFieldIds);
    }

    private static void EnsureSourceRequest(DashboardAnalyticsRequest request)
    {
        if (request is null || request.Source is null || request.Source.FormId == Guid.Empty)
        {
            throw new DashboardAnalyticsException(
                StatusCodes.Status400BadRequest,
                "Dashboard analytics request is invalid.",
                new[]
                {
                    new DashboardAnalyticsValidationError("source.formId", "dashboard.analytics.source.form_required", "Source form is required.")
                });
        }
    }

    private static DashboardAnalyticsRequest EnsureVisibleRequest(
        DashboardAnalyticsRequest request,
        IReadOnlySet<string> hiddenFieldIds)
    {
        var sanitized = NormalizeRequest(request);

        if (hiddenFieldIds.Count == 0)
        {
            return sanitized;
        }

        var hiddenMetric = sanitized.Metric.FieldId is not null && hiddenFieldIds.Contains(sanitized.Metric.FieldId);
        var hiddenGroup = sanitized.GroupByFieldId is not null && hiddenFieldIds.Contains(sanitized.GroupByFieldId);
        var hiddenDate = sanitized.DateFieldId is not null && hiddenFieldIds.Contains(sanitized.DateFieldId);
        var hiddenColumn = (sanitized.Columns ?? Array.Empty<string>()).Any(hiddenFieldIds.Contains);

        if (hiddenMetric || hiddenGroup || hiddenDate || hiddenColumn)
        {
            throw new DashboardAnalyticsException(StatusCodes.Status403Forbidden, "Dashboard analytics request references a hidden field.");
        }

        return sanitized;
    }

    private static DashboardAnalyticsRequest NormalizeRequest(DashboardAnalyticsRequest request)
    {
        return request with
        {
            WidgetType = request.WidgetType.Trim(),
            Metric = new DashboardAnalyticsMetricDefinition(request.Metric.Type.Trim(), NormalizeOptional(request.Metric.FieldId)),
            GroupByFieldId = NormalizeOptional(request.GroupByFieldId),
            DateFieldId = NormalizeOptional(request.DateFieldId),
            Columns = (request.Columns ?? Array.Empty<string>())
                .Select(column => column.Trim())
                .Where(column => column.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            Limit = request.Limit ?? 10
        };
    }

    private static ChartWidgetConfigDefinition ToChartConfig(DashboardAnalyticsRequest request)
    {
        return new ChartWidgetConfigDefinition(
            ToChartWidgetType(request.WidgetType),
            new ChartMetricDefinition(request.Metric.Type, request.Metric.FieldId),
            request.GroupByFieldId,
            request.DateFieldId,
            request.Columns,
            request.Limit,
            request.Source.ReportId);
    }

    private static string ToChartWidgetType(string widgetType)
    {
        return widgetType switch
        {
            DashboardAnalyticsWidgetTypes.Summary => ChartWidgetTypes.NumberCard,
            DashboardAnalyticsWidgetTypes.Breakdown => ChartWidgetTypes.ChoiceBreakdown,
            DashboardAnalyticsWidgetTypes.Trend => ChartWidgetTypes.DateTrend,
            DashboardAnalyticsWidgetTypes.Table => ChartWidgetTypes.Table,
            _ => widgetType
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

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
