using System.Text;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ListReportCsvExporter
{
    public const string ContentType = "text/csv; charset=utf-8";

    public static ListReportCsvExportDto Export(ListReportExecutionDto report)
    {
        var builder = new StringBuilder();
        AppendRow(builder, report.Columns.Select(column => column.Label));

        foreach (var row in report.Rows)
        {
            AppendRow(
                builder,
                report.Columns.Select(column => row.Cells.TryGetValue(column.FieldId, out var cell) ? cell.DisplayValue : string.Empty));
        }

        return new ListReportCsvExportDto(ToSafeFileName(report.ReportName), builder.ToString());
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<string> values)
    {
        builder.AppendJoin(',', values.Select(EscapeField));
        builder.Append("\r\n");
    }

    private static string EscapeField(string value)
    {
        var mustQuote = value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal)
            || value.Length != value.Trim().Length;

        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }

    private static string ToSafeFileName(string reportName)
    {
        var builder = new StringBuilder();
        var previousWasDash = false;

        foreach (var character in reportName.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
                continue;
            }

            if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var safeName = builder.ToString().Trim('-');
        return $"{(string.IsNullOrWhiteSpace(safeName) ? "report" : safeName)}.csv";
    }
}
