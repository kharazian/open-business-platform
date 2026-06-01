using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
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
        Guid formId,
        ChartWidgetConfigDefinition request,
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

        var sourceReportConfig = await GetSourceReportConfigAsync(formId, request.ReportId, schema, cancellationToken);
        var records = await dbContext.Records
            .AsNoTracking()
            .Where(record => record.FormId == formId && !record.IsDeleted && record.Status != RecordStatuses.Deleted)
            .ToArrayAsync(cancellationToken);

        return ChartAggregationEngine.Execute(form.Id, form.Name, request, schema, records, sourceReportConfig);
    }

    private async Task<ListReportConfigDefinition?> GetSourceReportConfigAsync(
        Guid formId,
        Guid? reportId,
        FormSchemaDefinition schema,
        CancellationToken cancellationToken)
    {
        if (reportId is null)
        {
            return null;
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

        return config;
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
