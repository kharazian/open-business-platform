using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.CreatorAnalysis;

public sealed class CreatorAnalysisService(
    OpenBusinessPlatformDbContext dbContext,
    CreatorExportAnalyzer analyzer,
    ILogger<CreatorAnalysisService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CreatorAnalysisReportDto> AnalyzeAsync(IFormFile file, Guid? actorId, CancellationToken ct)
    {
        CreatorAnalysisInputValidator.ValidateMetadata(file.FileName, file.ContentType, file.Length);
        var bytes = await ReadBoundedAsync(file, ct);
        var source = CreatorAnalysisInputValidator.DecodeAndValidate(bytes);

        var report = analyzer.Analyze(source, bytes.Length);
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(), EntityType = "CreatorAnalysis", EntityId = Guid.NewGuid(), Action = "creator_export_analyzed", UserId = actorId,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                analyzerVersion = report.AnalyzerVersion,
                report.Source.ByteCount,
                report.Source.LineCount,
                report.Summary.ConstructCount,
                report.Summary.FindingCount,
                report.Summary.ByStatus,
                credentialCategories = report.CredentialSignals.ToDictionary(item => item.Category, item => item.Count, StringComparer.Ordinal),
                report.Complete,
                report.Truncated,
                sourceFingerprint = fingerprint
            }, JsonOptions)
        });
        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation(
            "Creator export analysis {AnalyzerVersion} completed: {ByteCount} bytes, {LineCount} lines, {ConstructCount} constructs, {FindingCount} findings, complete {Complete}, truncated {Truncated}.",
            report.AnalyzerVersion, report.Source.ByteCount, report.Source.LineCount, report.Summary.ConstructCount, report.Summary.FindingCount, report.Complete, report.Truncated);
        return report;
    }

    private static async Task<byte[]> ReadBoundedAsync(IFormFile file, CancellationToken ct)
    {
        await using var input = file.OpenReadStream();
        using var output = new MemoryStream(Math.Min((int)file.Length, CreatorAnalysisLimits.MaxSourceBytes));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer, ct);
            if (read == 0) break;
            if (output.Length + read > CreatorAnalysisLimits.MaxSourceBytes) throw TooLarge();
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return output.ToArray();
    }

    private static CreatorAnalysisException TooLarge() => new(StatusCodes.Status413PayloadTooLarge, "Creator analysis source exceeds the size limit.");
}

public static class CreatorAnalysisInputValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static void ValidateMetadata(string fileName, string contentType, long length)
    {
        if (length == 0) throw Invalid("Creator analysis source is required.");
        if (length > CreatorAnalysisLimits.MaxSourceBytes) throw TooLarge();
        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".ds", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            throw Invalid("Creator analysis accepts .ds or .txt UTF-8 text files.");
        var normalizedContentType = contentType.Split(';', 2)[0].Trim();
        if (!normalizedContentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            throw Invalid("Creator analysis requires a plain-text source.");
    }

    public static string DecodeAndValidate(ReadOnlySpan<byte> bytes)
    {
        string source;
        try { source = StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException) { throw Invalid("Creator analysis requires valid UTF-8 text."); }
        ValidateText(source);
        return source;
    }

    private static void ValidateText(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) throw Invalid("Creator analysis source is required.");
        var lineCount = 1;
        var previousWasCarriageReturn = false;
        foreach (var character in source)
        {
            if (character == '\0' || char.IsControl(character) && character is not '\r' and not '\n' and not '\t')
                throw Invalid("Creator analysis requires plain UTF-8 text.");
            if (character == '\r' || character == '\n' && !previousWasCarriageReturn)
                lineCount++;
            previousWasCarriageReturn = character == '\r';
            if (lineCount > CreatorAnalysisLimits.MaxLines)
                throw Invalid("Creator analysis source exceeds the line limit.");
        }
    }

    private static CreatorAnalysisException Invalid(string message) => new(StatusCodes.Status400BadRequest, message);
    private static CreatorAnalysisException TooLarge() => new(StatusCodes.Status413PayloadTooLarge, "Creator analysis source exceeds the size limit.");
}
