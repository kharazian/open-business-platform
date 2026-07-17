using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class OidcSsoService(
    OpenBusinessPlatformDbContext dbContext,
    SsoProviderService providers,
    WorkspaceMembershipService memberships,
    IDataProtectionProvider dataProtectionProvider,
    IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(5);
    private readonly IDataProtector stateProtector = dataProtectionProvider.CreateProtector("OpenBusinessPlatform.SsoState.v1");

    public async Task<StartSsoResponse> StartAsync(StartSsoRequest request, CancellationToken cancellationToken)
    {
        var (provider, workspaceId) = await providers.ResolveEnabledAsync(
            request.TenantSlug,
            request.WorkspaceSlug,
            request.ProviderKey,
            cancellationToken);
        var configuration = await GetConfigurationAsync(provider, cancellationToken);
        EnsureHttpsEndpoint(configuration.AuthorizationEndpoint, "authorization");

        var verifier = SsoPolicy.CreateRandomValue(48);
        var state = new SsoFlowState(
            workspaceId,
            provider.Id,
            SsoPolicy.CreateRandomValue(),
            verifier,
            SsoPolicy.NormalizeReturnPath(request.ReturnPath),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var protectedState = stateProtector.Protect(JsonSerializer.Serialize(state));
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = provider.CallbackUrl,
            ["scope"] = "openid profile email",
            ["state"] = protectedState,
            ["nonce"] = state.Nonce,
            ["code_challenge"] = SsoPolicy.CreateCodeChallenge(verifier),
            ["code_challenge_method"] = "S256"
        };
        return new StartSsoResponse(QueryHelpers.AddQueryString(configuration.AuthorizationEndpoint, query));
    }

    public async Task<SsoCallbackResult> CompleteAsync(
        string? code,
        string? protectedState,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(protectedState))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "The SSO callback is incomplete.");
        }

        var state = ReadState(protectedState);
        var provider = await dbContext.SsoProviders.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == state.ProviderId
                && item.WorkspaceId == state.WorkspaceId
                && item.IsEnabled,
                cancellationToken)
            ?? throw new SsoException(StatusCodes.Status400BadRequest, "The SSO provider is no longer available.");
        var configuration = await GetConfigurationAsync(provider, cancellationToken);
        EnsureHttpsEndpoint(configuration.TokenEndpoint, "token");
        var idToken = await ExchangeCodeAsync(provider, configuration.TokenEndpoint, code, state.CodeVerifier, cancellationToken);
        var externalPrincipal = ValidateIdToken(provider, configuration, idToken, state.Nonce);
        var subject = RequiredClaim(externalPrincipal, "sub");
        var email = RequiredClaim(externalPrincipal, "email").Trim().ToLowerInvariant();
        var emailVerified = externalPrincipal.FindFirstValue("email_verified");
        if (!string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new SsoException(StatusCodes.Status403Forbidden, "The identity provider did not verify this email address.");
        }

        var identity = await dbContext.ExternalIdentities.IgnoreQueryFilters()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item =>
                item.WorkspaceId == state.WorkspaceId
                && item.ProviderId == provider.Id
                && item.Subject == subject,
                cancellationToken);
        var user = identity?.User;
        if (user is null)
        {
            user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email == email && item.IsActive, cancellationToken)
                ?? throw new SsoException(StatusCodes.Status403Forbidden, "No active platform user is linked to this SSO identity.");
        }
        if (!user.IsActive || !await memberships.IsActiveMemberAsync(user.Id, state.WorkspaceId, cancellationToken))
        {
            throw new SsoException(StatusCodes.Status403Forbidden, "Active workspace membership is required.");
        }

        var roleNames = await memberships.GetRoleNamesAsync(user.Id, state.WorkspaceId, cancellationToken);
        var platformUser = new AuthenticatedUser(user.Id.ToString(), user.Name, user.Email, roleNames);
        var principal = IdentityPrincipalFactory.Create(platformUser, state.WorkspaceId);
        httpContext.User = principal;
        var now = DateTimeOffset.UtcNow;
        if (identity is null)
        {
            identity = new ExternalIdentity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = state.WorkspaceId,
                ProviderId = provider.Id,
                UserId = user.Id,
                Subject = subject,
                EmailAtLink = email,
                LastSignedInAt = now,
                CreatedById = user.Id
            };
            dbContext.ExternalIdentities.Add(identity);
            dbContext.AuditLogs.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "ExternalIdentity",
                EntityId = identity.Id,
                Action = "external_identity_linked",
                UserId = user.Id,
                MetadataJson = JsonSerializer.SerializeToDocument(new { providerId = provider.Id, userId = user.Id })
            });
        }
        else
        {
            identity.LastSignedInAt = now;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SsoCallbackResult(principal, state.ReturnPath);
    }

    private SsoFlowState ReadState(string protectedState)
    {
        try
        {
            var state = JsonSerializer.Deserialize<SsoFlowState>(stateProtector.Unprotect(protectedState))
                ?? throw new SsoException(StatusCodes.Status400BadRequest, "The SSO state is invalid.");
            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(state.IssuedAtUnixSeconds);
            if (issuedAt > DateTimeOffset.UtcNow.AddMinutes(1) || DateTimeOffset.UtcNow - issuedAt > StateLifetime)
            {
                throw new SsoException(StatusCodes.Status400BadRequest, "The SSO sign-in request expired.");
            }
            if (state.WorkspaceId == Guid.Empty || state.ProviderId == Guid.Empty
                || string.IsNullOrWhiteSpace(state.Nonce) || string.IsNullOrWhiteSpace(state.CodeVerifier))
            {
                throw new SsoException(StatusCodes.Status400BadRequest, "The SSO state is invalid.");
            }
            return state with { ReturnPath = SsoPolicy.NormalizeReturnPath(state.ReturnPath) };
        }
        catch (SsoException)
        {
            throw;
        }
        catch
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "The SSO state is invalid.");
        }
    }

    private async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        SsoProvider provider,
        CancellationToken cancellationToken)
    {
        var metadataAddress = $"{provider.Issuer.TrimEnd('/')}/.well-known/openid-configuration";
        var retriever = new HttpDocumentRetriever(httpClientFactory.CreateClient("oidc-discovery")) { RequireHttps = true };
        var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            retriever);
        var configuration = await manager.GetConfigurationAsync(cancellationToken);
        if (!string.Equals(configuration.Issuer?.TrimEnd('/'), provider.Issuer.TrimEnd('/'), StringComparison.Ordinal))
        {
            throw new SsoException(StatusCodes.Status503ServiceUnavailable, "The identity provider issuer does not match its configuration.");
        }
        return configuration;
    }

    private async Task<string> ExchangeCodeAsync(
        SsoProvider provider,
        string tokenEndpoint,
        string code,
        string verifier,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = provider.ClientId,
            ["client_secret"] = providers.GetClientSecret(provider),
            ["code"] = code,
            ["redirect_uri"] = provider.CallbackUrl,
            ["code_verifier"] = verifier
        });
        using var response = await httpClientFactory.CreateClient("oidc-token").PostAsync(tokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new SsoException(StatusCodes.Status401Unauthorized, "The identity provider rejected the authorization code.");
        }
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return payload.RootElement.TryGetProperty("id_token", out var token) && token.ValueKind == JsonValueKind.String
            ? token.GetString()!
            : throw new SsoException(StatusCodes.Status401Unauthorized, "The identity provider did not return an ID token.");
    }

    private static ClaimsPrincipal ValidateIdToken(
        SsoProvider provider,
        OpenIdConnectConfiguration configuration,
        string idToken,
        string expectedNonce)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(idToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = provider.Issuer,
                ValidateAudience = true,
                ValidAudience = provider.ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            }, out _);
            if (!string.Equals(principal.FindFirstValue("nonce"), expectedNonce, StringComparison.Ordinal))
            {
                throw new SecurityTokenValidationException("OIDC nonce mismatch.");
            }
            _ = RequiredClaim(principal, "sub");
            return principal;
        }
        catch (SsoException)
        {
            throw;
        }
        catch
        {
            throw new SsoException(StatusCodes.Status401Unauthorized, "The identity provider token could not be validated.");
        }
    }

    private static string RequiredClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return string.IsNullOrWhiteSpace(value)
            ? throw new SsoException(StatusCodes.Status401Unauthorized, $"The identity provider token is missing {claimType}.")
            : value;
    }

    private static void EnsureHttpsEndpoint(string? value, string label)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new SsoException(StatusCodes.Status503ServiceUnavailable, $"The identity provider {label} endpoint is invalid.");
        }
    }
}
