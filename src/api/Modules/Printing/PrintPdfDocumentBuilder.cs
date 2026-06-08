using System.Globalization;
using System.Text;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public sealed record PrintPdfDocument(
    string Title,
    IReadOnlyList<string> Metadata,
    IReadOnlyList<PrintPdfSection> Sections,
    string? Footer);

public sealed record PrintPdfSection(string Title, IReadOnlyList<string> Lines);

public static class PrintPdfDocumentBuilder
{
    private const int PageWidth = 612;
    private const int PageHeight = 792;
    private const int MarginLeft = 54;
    private const int StartY = 742;
    private const int LineHeight = 16;
    private const int BottomY = 72;
    private const int MaxLineLength = 92;

    public const string ContentType = "application/pdf";

    public static byte[] Build(PrintPdfDocument document)
    {
        var pages = Paginate(document).ToArray();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Length).Select(index => $"{3 + index * 2} 0 R"))}] /Count {pages.Length} >>"
        };

        for (var index = 0; index < pages.Length; index += 1)
        {
            var pageObjectNumber = 3 + index * 2;
            var contentObjectNumber = pageObjectNumber + 1;
            var stream = BuildPageStream(pages[index]);
            var streamByteLength = Encoding.ASCII.GetByteCount(stream);

            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 {3 + pages.Length * 2} 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {streamByteLength} >>\nstream\n{stream}endstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        return WritePdf(objects);
    }

    private static IEnumerable<IReadOnlyList<PdfTextLine>> Paginate(PrintPdfDocument document)
    {
        var pages = new List<IReadOnlyList<PdfTextLine>>();
        var currentPage = new List<PdfTextLine>();
        var y = StartY;

        void AddLine(string text, int fontSize = 10)
        {
            foreach (var line in WrapLine(text))
            {
                if (y < BottomY)
                {
                    yieldPage();
                }

                currentPage.Add(new PdfTextLine(line, fontSize, y));
                y -= LineHeight;
            }
        }

        void yieldPage()
        {
            if (currentPage.Count == 0)
            {
                return;
            }

            pages.Add(currentPage.ToArray());
            currentPage.Clear();
            y = StartY;
        }

        AddLine(NormalizeText(document.Title), 16);

        foreach (var metadata in document.Metadata.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            AddLine(NormalizeText(metadata), 9);
        }

        AddLine(string.Empty);

        foreach (var section in document.Sections)
        {
            AddLine(NormalizeText(section.Title), 12);

            foreach (var line in section.Lines)
            {
                AddLine(NormalizeText(line), 10);
            }

            AddLine(string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(document.Footer))
        {
            AddLine(NormalizeText(document.Footer), 9);
        }

        yieldPage();
        return pages.Count > 0 ? pages : new[] { Array.Empty<PdfTextLine>() };
    }

    private static string BuildPageStream(IReadOnlyList<PdfTextLine> lines)
    {
        var builder = new StringBuilder();

        foreach (var line in lines)
        {
            builder
                .Append("BT /F1 ")
                .Append(line.FontSize.ToString(CultureInfo.InvariantCulture))
                .Append(" Tf ")
                .Append(MarginLeft.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(line.Y.ToString(CultureInfo.InvariantCulture))
                .Append(" Td (")
                .Append(EscapePdfText(line.Text))
                .AppendLine(") Tj ET");
        }

        return builder.ToString();
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects)
    {
        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };

        builder.AppendLine("%PDF-1.4");

        for (var index = 0; index < objects.Count; index += 1)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder
                .Append(index + 1)
                .AppendLine(" 0 obj")
                .AppendLine(objects[index])
                .AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder
            .AppendLine("xref")
            .Append("0 ")
            .AppendLine((objects.Count + 1).ToString(CultureInfo.InvariantCulture))
            .AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).AppendLine(" 00000 n ");
        }

        builder
            .AppendLine("trailer")
            .Append("<< /Size ")
            .Append(objects.Count + 1)
            .AppendLine(" /Root 1 0 R >>")
            .AppendLine("startxref")
            .AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture))
            .AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static IEnumerable<string> WrapLine(string text)
    {
        var normalized = NormalizeText(text);

        if (normalized.Length <= MaxLineLength)
        {
            yield return normalized;
            yield break;
        }

        for (var index = 0; index < normalized.Length; index += MaxLineLength)
        {
            yield return normalized.Substring(index, Math.Min(MaxLineLength, normalized.Length - index));
        }
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return new string(value.Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray());
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private sealed record PdfTextLine(string Text, int FontSize, int Y);
}
