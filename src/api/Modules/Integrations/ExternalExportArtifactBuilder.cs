using System.Text.Json;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class ExternalExportArtifactBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static ExternalExportArtifact Build(string format, ListReportExecutionDto report)
    {
        return format switch
        {
            ExternalExportJobFormats.Csv => BuildCsv(report),
            ExternalExportJobFormats.Json => BuildJson(report),
            _ => throw new ExternalExportException(StatusCodes.Status400BadRequest, "Export format is invalid.")
        };
    }

    private static ExternalExportArtifact BuildCsv(ListReportExecutionDto report)
    {
        var csv = ListReportCsvExporter.Export(report);
        return ToArtifact(csv.FileName, ListReportCsvExporter.ContentType, csv.Content);
    }

    private static ExternalExportArtifact BuildJson(ListReportExecutionDto report)
    {
        var payload = new
        {
            report.ReportId,
            report.FormId,
            report.ReportName,
            report.FormName,
            report.TotalCount,
            columns = report.Columns.Select(column => new
            {
                column.FieldId,
                column.Label,
                column.Type,
                column.Source
            }),
            rows = report.Rows.Select(row => new
            {
                row.RecordId,
                row.Status,
                row.CreatedAt,
                values = report.Columns.ToDictionary(
                    column => column.FieldId,
                    column => row.Cells.TryGetValue(column.FieldId, out var cell) ? cell.Value : null,
                    StringComparer.Ordinal)
            })
        };
        var content = JsonSerializer.Serialize(payload, JsonOptions);
        return ToArtifact(ToSafeFileName(report.ReportName, "json"), "application/json; charset=utf-8", content);
    }

    private static ExternalExportArtifact ToArtifact(string fileName, string contentType, string content)
    {
        return new ExternalExportArtifact(
            fileName,
            contentType,
            content,
            System.Text.Encoding.UTF8.GetByteCount(content));
    }

    private static string ToSafeFileName(string name, string extension)
    {
        var safe = string.Concat(name.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '-')).Trim('-');

        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }

        return $"{(string.IsNullOrWhiteSpace(safe) ? "export" : safe)}.{extension}";
    }
}
