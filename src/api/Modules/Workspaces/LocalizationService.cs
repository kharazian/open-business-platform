using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed record WorkspaceLocalizationDto(string DefaultLocale, string DefaultTimeZone, int FirstDayOfWeek, string? ConcurrencyStamp);
public sealed record UserLocalizationPreferenceDto(string? Locale, string? TimeZone, string? ConcurrencyStamp);
public sealed record LocalizationSettingsDto(WorkspaceLocalizationDto Workspace, UserLocalizationPreferenceDto User, string EffectiveLocale, string EffectiveTimeZone);
public sealed record SaveWorkspaceLocalizationRequest(string DefaultLocale, string DefaultTimeZone, int FirstDayOfWeek, string? ConcurrencyStamp);
public sealed record SaveUserLocalizationPreferenceRequest(string? Locale, string? TimeZone, string? ConcurrencyStamp);

public sealed class LocalizationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class LocalizationService(OpenBusinessPlatformDbContext dbContext)
{
    public const string FallbackLocale = "en-CA";
    public const string FallbackTimeZone = "UTC";
    public const int FallbackFirstDayOfWeek = 1;

    public async Task<LocalizationSettingsDto> GetCurrentAsync(Guid? userId, CancellationToken ct)
    {
        var workspace = await dbContext.WorkspaceLocalizations.AsNoTracking().SingleOrDefaultAsync(ct);
        var preference = userId is null ? null : await dbContext.UserLocalizationPreferences.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, ct);
        return Resolve(workspace, preference);
    }

    public async Task<LocalizationSettingsDto> SaveWorkspaceAsync(SaveWorkspaceLocalizationRequest request, Guid? actorId, CancellationToken ct)
    {
        var locale = ValidateLocale(request.DefaultLocale, false)!;
        var timeZone = ValidateTimeZone(request.DefaultTimeZone, false)!;
        if (request.FirstDayOfWeek is < 0 or > 6) throw new LocalizationException(400, "First day of week must be between 0 and 6.");
        var item = await dbContext.WorkspaceLocalizations.SingleOrDefaultAsync(ct);
        var created = item is null;
        if (item is null)
        {
            if (!string.IsNullOrWhiteSpace(request.ConcurrencyStamp)) throw Conflict();
            item = new WorkspaceLocalization { Id = Guid.NewGuid(), CreatedById = actorId };
            dbContext.WorkspaceLocalizations.Add(item);
        }
        else
        {
            EnsureConcurrency(item.ConcurrencyStamp, request.ConcurrencyStamp);
            item.UpdatedById = actorId;
        }
        item.DefaultLocale = locale;
        item.DefaultTimeZone = timeZone;
        item.FirstDayOfWeek = request.FirstDayOfWeek;
        Audit(item.Id, created ? "workspace_localization_created" : "workspace_localization_updated", actorId, new { locale, timeZone, request.FirstDayOfWeek });
        await SaveAsync(ct);
        return await GetCurrentAsync(actorId, ct);
    }

    public async Task<LocalizationSettingsDto> SaveUserAsync(Guid? userId, SaveUserLocalizationPreferenceRequest request, CancellationToken ct)
    {
        if (userId is null) throw new LocalizationException(403, "A persisted user is required to save localization preferences.");
        var locale = ValidateLocale(request.Locale, true);
        var timeZone = ValidateTimeZone(request.TimeZone, true);
        var item = await dbContext.UserLocalizationPreferences.SingleOrDefaultAsync(entry => entry.UserId == userId, ct);
        var created = item is null;
        if (item is null)
        {
            if (!string.IsNullOrWhiteSpace(request.ConcurrencyStamp)) throw Conflict();
            item = new UserLocalizationPreference { Id = Guid.NewGuid(), UserId = userId.Value, CreatedById = userId };
            dbContext.UserLocalizationPreferences.Add(item);
        }
        else
        {
            EnsureConcurrency(item.ConcurrencyStamp, request.ConcurrencyStamp);
            item.UpdatedById = userId;
        }
        item.Locale = locale;
        item.TimeZone = timeZone;
        Audit(item.Id, created ? "user_localization_created" : "user_localization_updated", userId, new { locale, timeZone });
        await SaveAsync(ct);
        return await GetCurrentAsync(userId, ct);
    }

    public static LocalizationSettingsDto Resolve(WorkspaceLocalization? workspace, UserLocalizationPreference? user)
    {
        var workspaceDto = new WorkspaceLocalizationDto(workspace?.DefaultLocale ?? FallbackLocale, workspace?.DefaultTimeZone ?? FallbackTimeZone, workspace?.FirstDayOfWeek ?? FallbackFirstDayOfWeek, workspace?.ConcurrencyStamp);
        var userDto = new UserLocalizationPreferenceDto(user?.Locale, user?.TimeZone, user?.ConcurrencyStamp);
        return new(workspaceDto, userDto, user?.Locale ?? workspaceDto.DefaultLocale, user?.TimeZone ?? workspaceDto.DefaultTimeZone);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw Conflict(); }
    }

    private void Audit(Guid id, string action, Guid? actorId, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry
    {
        Id = Guid.NewGuid(), EntityType = "Localization", EntityId = id, Action = action, UserId = actorId,
        MetadataJson = JsonSerializer.SerializeToDocument(metadata)
    });
    private static void EnsureConcurrency(string current, string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested) || current != requested.Trim()) throw new DbUpdateConcurrencyException("Localization settings changed. Refresh and try again.");
    }
    private static LocalizationException Conflict() => new(409, "Localization settings were created by another request. Refresh and try again.");
    private static string? ValidateLocale(string? value, bool optional)
    {
        if (string.IsNullOrWhiteSpace(value)) return optional ? null : throw new LocalizationException(400, "Locale is required.");
        var normalized = value.Trim();
        if (normalized.Length > 35) throw new LocalizationException(400, "Locale must be at most 35 characters.");
        try { return CultureInfo.GetCultureInfo(normalized).Name; }
        catch (CultureNotFoundException) { throw new LocalizationException(400, "Locale is not supported by the server."); }
    }
    private static string? ValidateTimeZone(string? value, bool optional)
    {
        if (string.IsNullOrWhiteSpace(value)) return optional ? null : throw new LocalizationException(400, "Timezone is required.");
        var normalized = value.Trim();
        if (normalized.Length > 120) throw new LocalizationException(400, "Timezone must be at most 120 characters.");
        try { return TimeZoneInfo.FindSystemTimeZoneById(normalized).Id; }
        catch (TimeZoneNotFoundException) { throw new LocalizationException(400, "Timezone is not supported by the server."); }
        catch (InvalidTimeZoneException) { throw new LocalizationException(400, "Timezone is not valid."); }
    }
}
