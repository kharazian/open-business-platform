using System.Security.Claims;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class PublicRecordApiAccess
{
    public static bool HasScope(ClaimsPrincipal apiKeyPrincipal, string scope)
    {
        return apiKeyPrincipal.FindAll(IntegrationApiKeyClaims.Scope)
            .Any(claim => string.Equals(claim.Value, scope, StringComparison.Ordinal));
    }

    public static ClaimsPrincipal CreateEffectiveUserPrincipal(ClaimsPrincipal apiKeyPrincipal)
    {
        var userId = apiKeyPrincipal.FindFirstValue(IntegrationApiKeyClaims.CreatedByUserId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new IntegrationApiKeyException(
                StatusCodes.Status403Forbidden,
                "API key is not linked to a platform user for record permission checks.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, IntegrationApiKeyAuthenticationDefaults.AuthenticationScheme));
    }
}
