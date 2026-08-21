namespace OpenBusinessPlatform.Api.Modules.CreatorAnalysis;

public static class CreatorAnalysisLimits
{
    public const int MaxSourceBytes = 1024 * 1024;
    public const int MaxLines = 50_000;
    public const int MaxConstructs = 500;
    public const int MaxFindings = 1_000;
    public const string AnalyzerVersion = "creator-analysis-v1";
}

public static class CreatorAnalysisStatuses
{
    public const string Supported = "supported";
    public const string ManualReview = "manual_review";
    public const string Unsupported = "unsupported";
    public const string Unsafe = "unsafe";
    public const string Unknown = "unknown";
    public static IReadOnlyList<string> All { get; } = [Supported, ManualReview, Unsupported, Unsafe, Unknown];
}

public sealed record CreatorAnalysisSourceDto(int ByteCount, int LineCount);
public sealed record CreatorCredentialSignalDto(string Category, int Count);
public sealed record CreatorAnalysisConstructDto(
    string Id,
    string Type,
    string DisplayName,
    int LineStart,
    int LineEnd,
    string Status,
    string? ProposedModule,
    string? ProposedType);
public sealed record CreatorAnalysisFindingDto(
    string Id,
    string Severity,
    string Status,
    string ReasonCode,
    string? ConstructId,
    string Message);
public sealed record CreatorAnalysisSummaryDto(
    int ConstructCount,
    int FindingCount,
    IReadOnlyDictionary<string, int> ByStatus);
public sealed record CreatorAnalysisReportDto(
    string AnalyzerVersion,
    bool CanImport,
    bool Complete,
    bool Truncated,
    CreatorAnalysisSourceDto Source,
    CreatorAnalysisSummaryDto Summary,
    IReadOnlyList<CreatorCredentialSignalDto> CredentialSignals,
    IReadOnlyList<CreatorAnalysisConstructDto> Constructs,
    IReadOnlyList<CreatorAnalysisFindingDto> Findings);

public sealed class CreatorAnalysisException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
