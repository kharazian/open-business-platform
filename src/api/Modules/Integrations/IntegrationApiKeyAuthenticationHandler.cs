using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IntegrationApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-OBP-API-Key";
    private const string BearerPrefix = "Bearer ";

    private readonly IntegrationApiKeyService apiKeys;

    public IntegrationApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IntegrationApiKeyService apiKeys)
        : base(options, logger, encoder)
    {
        this.apiKeys = apiKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawKey = GetRawKey();

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return AuthenticateResult.NoResult();
        }

        var result = await apiKeys.AuthenticateAsync(
            rawKey,
            new IntegrationApiKeyUsageContext(
                Context.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.FirstOrDefault()),
            Context.RequestAborted);

        if (!result.Succeeded || result.Principal is null)
        {
            return AuthenticateResult.Fail(result.FailureReason ?? "API key authentication failed.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(result.Principal, Scheme.Name));
    }

    private string? GetRawKey()
    {
        var authorization = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[BearerPrefix.Length..].Trim();
        }

        return Request.Headers[ApiKeyHeaderName].FirstOrDefault()?.Trim();
    }
}
