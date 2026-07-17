using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed record PublicWorkspaceBrandingDto(string AppName, string LogoText, string? LogoDataUrl, string PrimaryColor, string? LoginMessage);
public sealed record WorkspaceBrandingDto(string AppName, string LogoText, string? LogoDataUrl, string PrimaryColor, string? LoginMessage, string? ConcurrencyStamp);
public sealed record SaveWorkspaceBrandingRequest(string AppName, string LogoText, string? LogoDataUrl, string PrimaryColor, string? LoginMessage, string? ConcurrencyStamp);

public sealed class WorkspaceBrandingException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed partial class WorkspaceBrandingService(OpenBusinessPlatformDbContext dbContext)
{
    public const string DefaultAppName = "Open Business Platform";
    public const string DefaultLogoText = "OBP";
    public const string DefaultPrimaryColor = "#2563eb";
    private const int MaxImageBytes = 256 * 1024;

    public async Task<PublicWorkspaceBrandingDto?> GetPublicAsync(string? tenantSlug, string? workspaceSlug, CancellationToken ct)
    {
        var tenant = NormalizeSlug(tenantSlug);
        var workspace = NormalizeSlug(workspaceSlug);
        if (tenant is null || workspace is null) return null;

        var target = await dbContext.Workspaces.IgnoreQueryFilters()
            .Where(item => item.IsActive && item.Slug == workspace && item.Tenant != null && item.Tenant.IsActive && item.Tenant.Slug == tenant)
            .Select(item => new { item.Id, item.Name })
            .SingleOrDefaultAsync(ct);
        if (target is null) return null;

        var branding = await dbContext.WorkspaceBrandings.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item => item.WorkspaceId == target.Id, ct);
        var resolved = Resolve(branding, target.Name);
        return new(resolved.AppName, resolved.LogoText, resolved.LogoDataUrl, resolved.PrimaryColor, resolved.LoginMessage);
    }

    public async Task<WorkspaceBrandingDto> GetCurrentAsync(CancellationToken ct)
    {
        var branding = await dbContext.WorkspaceBrandings.AsNoTracking().SingleOrDefaultAsync(ct);
        var workspaceName = await dbContext.Workspaces.IgnoreQueryFilters()
            .Where(item => item.Id == dbContext.ActiveWorkspaceId)
            .Select(item => item.Name)
            .SingleOrDefaultAsync(ct);
        return Resolve(branding, workspaceName);
    }

    public async Task<WorkspaceBrandingDto> SaveCurrentAsync(SaveWorkspaceBrandingRequest request, Guid? actorId, CancellationToken ct)
    {
        var values = Validate(request);
        var branding = await dbContext.WorkspaceBrandings.SingleOrDefaultAsync(ct);
        var created = branding is null;

        if (branding is null)
        {
            if (!string.IsNullOrWhiteSpace(request.ConcurrencyStamp))
                throw new WorkspaceBrandingException(409, "Workspace branding was created by another request. Refresh and try again.");
            branding = new WorkspaceBranding { Id = Guid.NewGuid(), CreatedById = actorId };
            dbContext.WorkspaceBrandings.Add(branding);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp) || branding.ConcurrencyStamp != request.ConcurrencyStamp.Trim())
                throw new DbUpdateConcurrencyException("Workspace branding changed. Refresh and try again.");
            branding.UpdatedById = actorId;
        }

        branding.AppName = values.AppName;
        branding.LogoText = values.LogoText;
        branding.LogoDataUrl = values.LogoDataUrl;
        branding.PrimaryColor = values.PrimaryColor;
        branding.LoginMessage = values.LoginMessage;
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(), EntityType = "WorkspaceBranding", EntityId = branding.Id,
            Action = created ? "workspace_branding_created" : "workspace_branding_updated", UserId = actorId,
            MetadataJson = JsonSerializer.SerializeToDocument(new { branding.AppName, branding.LogoText, branding.PrimaryColor, hasLogo = branding.LogoDataUrl is not null })
        });
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new WorkspaceBrandingException(409, "Workspace branding was created by another request. Refresh and try again.");
        }
        return Resolve(branding, null);
    }

    public static WorkspaceBrandingDto Resolve(WorkspaceBranding? branding, string? workspaceName) => branding is null
        ? new(string.IsNullOrWhiteSpace(workspaceName) ? DefaultAppName : workspaceName, DefaultLogoText, null, DefaultPrimaryColor, null, null)
        : new(branding.AppName, branding.LogoText, branding.LogoDataUrl, branding.PrimaryColor, branding.LoginMessage, branding.ConcurrencyStamp);

    private static (string AppName, string LogoText, string? LogoDataUrl, string PrimaryColor, string? LoginMessage) Validate(SaveWorkspaceBrandingRequest request)
    {
        var appName = Required(request.AppName, "App name", 120);
        var logoText = Required(request.LogoText, "Logo text", 8);
        var primaryColor = request.PrimaryColor?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!HexColor().IsMatch(primaryColor)) throw new WorkspaceBrandingException(400, "Primary color must be a six-digit hexadecimal color.");
        var loginMessage = Optional(request.LoginMessage, 240, "Login message");
        var logo = Optional(request.LogoDataUrl, 400_000, "Logo");
        if (logo is not null) ValidateLogo(logo);
        return (appName, logoText, logo, primaryColor, loginMessage);
    }

    private static void ValidateLogo(string value)
    {
        var match = LogoDataUrl().Match(value);
        if (!match.Success) throw new WorkspaceBrandingException(400, "Logo must be a PNG, JPEG, or WebP data URL.");
        try
        {
            if (Convert.FromBase64String(match.Groups[1].Value).Length > MaxImageBytes)
                throw new WorkspaceBrandingException(400, "Logo must be 256 KiB or smaller.");
        }
        catch (FormatException)
        {
            throw new WorkspaceBrandingException(400, "Logo data is not valid base64.");
        }
    }

    private static string Required(string? value, string label, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max
        ? value.Trim() : throw new WorkspaceBrandingException(400, $"{label} is required and must be at most {max} characters.");
    private static string? Optional(string? value, int max, string label) => string.IsNullOrWhiteSpace(value) ? null
        : value.Trim().Length <= max ? value.Trim() : throw new WorkspaceBrandingException(400, $"{label} must be at most {max} characters.");
    private static string? NormalizeSlug(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    [GeneratedRegex("^#[0-9a-f]{6}$", RegexOptions.CultureInvariant)] private static partial Regex HexColor();
    [GeneratedRegex("^data:image/(?:png|jpeg|webp);base64,([A-Za-z0-9+/=]+)$", RegexOptions.CultureInvariant)] private static partial Regex LogoDataUrl();
}
