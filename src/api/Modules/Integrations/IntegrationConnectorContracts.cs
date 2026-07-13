namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IntegrationConnectorTypes
{
    public const string Sftp = "sftp";
    public const string FileStorage = "file_storage";
    public const string VendorApi = "vendor_api";
    public const string Webhook = "webhook";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Sftp,
        FileStorage,
        VendorApi,
        Webhook
    };
}

public sealed record IntegrationConnectorDto(
    Guid Id,
    string Name,
    string ConnectorKey,
    string Type,
    IReadOnlyDictionary<string, object?> Config,
    IReadOnlyList<string> ConfiguredSecretNames,
    bool IsActive,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record UpsertIntegrationConnectorRequest(
    string Name,
    string ConnectorKey,
    string Type,
    IReadOnlyDictionary<string, object?>? Config,
    IReadOnlyDictionary<string, string?>? Secrets,
    bool IsActive,
    string? ConcurrencyStamp = null);

public sealed record IntegrationConnectorErrorResponse(string Message);

public sealed class IntegrationConnectorException : Exception
{
    public IntegrationConnectorException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
