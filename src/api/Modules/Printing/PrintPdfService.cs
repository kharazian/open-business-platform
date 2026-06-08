using System.Globalization;
using System.Text.Json;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public sealed class PrintPdfService
{
    public byte[] BuildRecordPdf(PrintTemplateVersionDetailDto templateVersion, FormRecordDetailDto record)
    {
        var config = templateVersion.Config;
        var sections = config.Sections
            .Where(section => ShouldRenderRecordSection(section, record.Values))
            .Select(section => ToRecordSection(section, record))
            .Where(section => section is not null)
            .Select(section => section!)
            .ToArray();

        return PrintPdfDocumentBuilder.Build(new PrintPdfDocument(
            config.Header.Title,
            new[]
            {
                config.Header.Subtitle,
                $"Template version {templateVersion.VersionNumber}",
                $"Record {record.Id}",
                $"Form version {record.FormVersionId}",
                $"Generated {DateTimeOffset.UtcNow:u}"
            }.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToArray(),
            sections,
            config.Footer.Text));
    }

    public byte[] BuildReportPdf(PrintTemplateVersionDetailDto templateVersion, ListReportExecutionDto report)
    {
        var config = templateVersion.Config;
        var sections = config.Sections
            .Where(section => ShouldRenderReportSection(section, report))
            .Select(section => ToReportSection(section, report))
            .Where(section => section is not null)
            .Select(section => section!)
            .ToArray();

        return PrintPdfDocumentBuilder.Build(new PrintPdfDocument(
            config.Header.Title,
            new[]
            {
                config.Header.Subtitle,
                $"Template version {templateVersion.VersionNumber}",
                report.FormName,
                $"{report.TotalCount} rows",
                $"Page {report.Page}",
                $"Generated {DateTimeOffset.UtcNow:u}"
            }.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToArray(),
            sections,
            config.Footer.Text));
    }

    private static PrintPdfSection? ToRecordSection(PrintTemplateSectionConfig section, FormRecordDetailDto record)
    {
        if (section.Kind == PrintTemplateSectionKinds.Signature)
        {
            var signatureLines = (section.SignatureLabels ?? Array.Empty<string>())
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Select(label => $"{label}: ______________________________")
                .ToArray();

            return signatureLines.Length == 0 ? null : new PrintPdfSection(section.Title, signatureLines);
        }

        if (section.Kind != PrintTemplateSectionKinds.Fields)
        {
            return null;
        }

        var fieldsById = record.Schema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var fieldIds = section.FieldIds.Count > 0 ? section.FieldIds : record.Schema.Fields.Select(field => field.Id).ToArray();
        var lines = fieldIds
            .Where(fieldId => fieldsById.ContainsKey(fieldId))
            .Select(fieldId =>
            {
                var field = fieldsById[fieldId];
                record.Values.TryGetValue(fieldId, out var value);
                return $"{field.Label}: {FormatValue(value)}";
            })
            .ToArray();

        return lines.Length == 0 ? null : new PrintPdfSection(section.Title, lines);
    }

    private static PrintPdfSection? ToReportSection(PrintTemplateSectionConfig section, ListReportExecutionDto report)
    {
        if (section.Kind == PrintTemplateSectionKinds.Signature)
        {
            var signatureLines = (section.SignatureLabels ?? Array.Empty<string>())
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Select(label => $"{label}: ______________________________")
                .ToArray();

            return signatureLines.Length == 0 ? null : new PrintPdfSection(section.Title, signatureLines);
        }

        if (section.Kind != PrintTemplateSectionKinds.Table)
        {
            return null;
        }

        var columnsById = report.Columns.ToDictionary(column => column.FieldId, StringComparer.Ordinal);
        var columnIds = section.FieldIds.Count > 0 ? section.FieldIds : report.Columns.Select(column => column.FieldId).ToArray();
        var columns = columnIds
            .Where(fieldId => columnsById.ContainsKey(fieldId))
            .Select(fieldId => columnsById[fieldId])
            .ToArray();

        if (columns.Length == 0)
        {
            return null;
        }

        var lines = new List<string>
        {
            string.Join(" | ", columns.Select(column => column.Label))
        };

        lines.AddRange(report.Rows.Select(row =>
            string.Join(" | ", columns.Select(column => row.Cells.TryGetValue(column.FieldId, out var cell) ? cell.DisplayValue : "-"))));

        return new PrintPdfSection(section.Title, lines);
    }

    private static bool ShouldRenderRecordSection(PrintTemplateSectionConfig section, IReadOnlyDictionary<string, object?> values)
    {
        var conditions = section.Conditions ?? Array.Empty<PrintTemplateSectionConditionConfig>();
        return conditions.Count == 0
            || conditions.All(condition => MatchesCondition(FormatValue(values.TryGetValue(condition.FieldId, out var value) ? value : null), condition));
    }

    private static bool ShouldRenderReportSection(PrintTemplateSectionConfig section, ListReportExecutionDto report)
    {
        var conditions = section.Conditions ?? Array.Empty<PrintTemplateSectionConditionConfig>();
        return conditions.Count == 0
            || report.Rows.Any(row => conditions.All(condition =>
                MatchesCondition(row.Cells.TryGetValue(condition.FieldId, out var cell) ? cell.DisplayValue : string.Empty, condition)));
    }

    private static bool MatchesCondition(string value, PrintTemplateSectionConditionConfig condition)
    {
        var actual = value.Trim();
        var expected = (condition.Value ?? string.Empty).Trim();

        return condition.Operator switch
        {
            PrintTemplateConditionOperators.Equal => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            PrintTemplateConditionOperators.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            PrintTemplateConditionOperators.Contains => expected.Length > 0 && actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            PrintTemplateConditionOperators.IsEmpty => actual.Length == 0,
            PrintTemplateConditionOperators.IsNotEmpty => actual.Length > 0,
            _ => false
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "-",
            string text when string.IsNullOrWhiteSpace(text) => "-",
            string text => text,
            bool boolean => boolean ? "Yes" : "No",
            DateTimeOffset dateTime => dateTime.ToString("u", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            JsonElement json => FormatJsonElement(json),
            IEnumerable<object?> items => string.Join(", ", items.Select(FormatValue)),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "-"
        };
    }

    private static string FormatJsonElement(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString() ?? "-",
            JsonValueKind.Number => json.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Array => string.Join(", ", json.EnumerateArray().Select(FormatJsonElement)),
            JsonValueKind.Null => "-",
            JsonValueKind.Undefined => "-",
            _ => json.GetRawText()
        };
    }
}
