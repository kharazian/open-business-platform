using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class DefaultListReportFactory
{
    public const string DefaultReportName = "All Records";

    public static ListReportConfigDefinition CreateAllRecordsConfig(FormSchemaDefinition schema)
    {
        var columns = FormReportableFieldMetadata.GetReportableFields(schema)
            .Where(field => field.Source == ReportableFieldSources.Form
                || field.Id is ReportableSystemFields.Status or ReportableSystemFields.CreatedAt)
            .Select(field => new ListReportColumnDefinition(field.Id, field.Label, Visible: true, Width: GetDefaultWidth(field)))
            .ToArray();

        return new ListReportConfigDefinition(
            1,
            columns,
            Array.Empty<ListReportFilterDefinition>(),
            new[] { new ListReportSortDefinition(ReportableSystemFields.CreatedAt, ReportSortDirections.Desc) });
    }

    private static int GetDefaultWidth(ReportableFieldMetadata field)
    {
        return field.Type switch
        {
            FormFieldTypes.Textarea => 240,
            FormFieldTypes.Date or FormFieldTypes.Time => 140,
            FormFieldTypes.Datetime => 180,
            FormFieldTypes.Checkbox or FormFieldTypes.Rating => 120,
            _ => 180
        };
    }
}

public sealed class DefaultReportProvisioningService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public DefaultReportProvisioningService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task EnsureAllRecordsReportAsync(
        FormDefinition form,
        FormSchemaDefinition schema,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var reports = await dbContext.Reports
            .Where(report =>
                report.FormId == form.Id
                && report.Type == ReportTypes.List
                && !report.IsDeleted)
            .ToArrayAsync(cancellationToken);
        var generatedReport = reports.FirstOrDefault(IsGeneratedAllRecordsReport);

        if (generatedReport is not null)
        {
            generatedReport.ConfigJson = SerializeConfig(DefaultListReportFactory.CreateAllRecordsConfig(schema));
            generatedReport.UpdatedById = actorId;
            AddAudit("Report", generatedReport.Id, "default_report_refreshed", actorId);
            return;
        }

        if (reports.Any(report => string.Equals(report.Name, DefaultListReportFactory.DefaultReportName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var report = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Name = DefaultListReportFactory.DefaultReportName,
            Type = ReportTypes.List,
            ConfigJson = SerializeConfig(DefaultListReportFactory.CreateAllRecordsConfig(schema)),
            ExtraPropertiesJson = SerializeExtraProperties(new DefaultReportExtraProperties(true)),
            CreatedById = actorId
        };

        dbContext.Reports.Add(report);
        AddAudit("Report", report.Id, "default_report_created", actorId);
    }

    private static bool IsGeneratedAllRecordsReport(ReportDefinition report)
    {
        var properties = DeserializeExtraProperties(report.ExtraPropertiesJson);
        return properties?.IsDefaultAllRecords == true;
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

    private static JsonDocument SerializeConfig(ListReportConfigDefinition config)
    {
        return JsonSerializer.SerializeToDocument(config, JsonOptions);
    }

    private static JsonDocument SerializeExtraProperties(DefaultReportExtraProperties properties)
    {
        return JsonSerializer.SerializeToDocument(properties, JsonOptions);
    }

    private static DefaultReportExtraProperties? DeserializeExtraProperties(JsonDocument? json)
    {
        return json?.RootElement.Deserialize<DefaultReportExtraProperties>(JsonOptions);
    }

    private sealed record DefaultReportExtraProperties(bool IsDefaultAllRecords);
}
