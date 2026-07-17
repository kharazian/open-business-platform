using System.Text;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed record FileInspectionResult(bool Accepted, string? ContentType, string? ErrorCode, string? ErrorMessage);

public interface IFileAttachmentScanner
{
    FileInspectionResult Inspect(string fileName, string? declaredContentType, byte[] content, FormFieldFileUploadDefinition configuration);
}

public sealed class DeterministicFileAttachmentScanner : IFileAttachmentScanner
{
    public FileInspectionResult Inspect(string fileName, string? declaredContentType, byte[] content, FormFieldFileUploadDefinition configuration)
    {
        if (content.Length == 0) return Reject("attachment.empty", "The selected file is empty.");
        if (content.LongLength > configuration.MaxSizeBytes) return Reject("attachment.too_large", $"The selected file exceeds {configuration.MaxSizeBytes} bytes.");
        var detected = Detect(content);
        if (detected is null) return Reject("attachment.type_unsupported", "The selected file type is unsupported or its signature is invalid.");
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (detected == "text/plain" && extension == ".csv" && declaredContentType?.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase) == true) detected = "text/csv";
        if (!string.IsNullOrWhiteSpace(declaredContentType) && !MatchesDeclared(declaredContentType, detected))
            return Reject("attachment.type_mismatch", "The selected file content does not match its declared type.");
        if (!AllowedExtensions(detected).Contains(extension, StringComparer.Ordinal))
            return Reject("attachment.extension_mismatch", "The file extension does not match the inspected content type.");
        var allowed = configuration.AllowedContentTypes is { Count: > 0 } ? configuration.AllowedContentTypes : FormFileUploadLimits.SupportedContentTypes;
        return allowed.Contains(detected, StringComparer.Ordinal)
            ? new(true, detected, null, null)
            : Reject("attachment.type_not_allowed", "This field does not allow the selected file type.");
    }

    private static string? Detect(byte[] content)
    {
        if (content.AsSpan().StartsWith("%PDF-"u8)) return "application/pdf";
        if (content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (content.Length >= 3 && content[0] == 255 && content[1] == 216 && content[2] == 255) return "image/jpeg";
        if (content.Length >= 12 && content.AsSpan(0, 4).SequenceEqual("RIFF"u8) && content.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        if (content.Contains((byte)0)) return null;
        try
        {
            _ = new UTF8Encoding(false, true).GetString(content);
            return LooksLikeCsv(content) ? "text/csv" : "text/plain";
        }
        catch (DecoderFallbackException) { return null; }
    }

    private static bool LooksLikeCsv(byte[] content)
    {
        var sample = Encoding.UTF8.GetString(content.AsSpan(0, Math.Min(content.Length, 4096)));
        return sample.Contains(',') && (sample.Contains('\n') || sample.Contains('\r'));
    }

    private static bool MatchesDeclared(string declared, string detected)
    {
        var normalized = declared.Split(';', 2)[0].Trim().ToLowerInvariant();
        return normalized == detected || normalized == "application/octet-stream" || (normalized == "text/csv" && detected == "text/plain");
    }

    private static IReadOnlyList<string> AllowedExtensions(string contentType) => contentType switch
    {
        "application/pdf" => [".pdf"],
        "image/png" => [".png"],
        "image/jpeg" => [".jpg", ".jpeg"],
        "image/webp" => [".webp"],
        "text/csv" => [".csv"],
        "text/plain" => [".txt", ".log", ".md"],
        _ => Array.Empty<string>()
    };

    private static FileInspectionResult Reject(string code, string message) => new(false, null, code, message);
}
