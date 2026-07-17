using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public static class CustomDomainStatuses
{
    public const string Pending = "pending";
    public const string Verified = "verified";
}

public sealed record CustomDomainDto(Guid Id, string Hostname, string Status, bool IsEnabled, string VerificationRecordName, string VerificationRecordValue, DateTimeOffset? VerifiedAt, DateTimeOffset? LastCheckedAt, string? LastFailure, string ConcurrencyStamp);
public sealed record CreateCustomDomainRequest(string Hostname);
public sealed record CustomDomainMutationRequest(string ConcurrencyStamp);

public sealed class CustomDomainException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public interface IDnsTxtResolver
{
    Task<IReadOnlyCollection<string>> ResolveAsync(string recordName, CancellationToken cancellationToken);
}

public sealed class CloudflareDnsTxtResolver(HttpClient httpClient) : IDnsTxtResolver
{
    public async Task<IReadOnlyCollection<string>> ResolveAsync(string recordName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://cloudflare-dns.com/dns-query?name={Uri.EscapeDataString(recordName)}&type=TXT");
        request.Headers.Accept.ParseAdd("application/dns-json");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("Answer", out var answers) || answers.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
        return answers.EnumerateArray()
            .Where(answer => answer.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String)
            .Select(answer => NormalizeTxt(answer.GetProperty("data").GetString()!)).ToArray();
    }

    public static string NormalizeTxt(string value) => value.Trim().Replace("\" \"", string.Empty, StringComparison.Ordinal).Trim('"');
}

public sealed class CustomDomainService(OpenBusinessPlatformDbContext dbContext, IDnsTxtResolver dnsResolver)
{
    public async Task<IReadOnlyCollection<CustomDomainDto>> ListAsync(CancellationToken ct) =>
        (await dbContext.WorkspaceCustomDomains.AsNoTracking().OrderBy(item => item.Hostname).ToArrayAsync(ct)).Select(ToDto).ToArray();

    public async Task<CustomDomainDto> CreateAsync(CreateCustomDomainRequest request, Guid? actorId, CancellationToken ct)
    {
        var hostname = NormalizeHostname(request.Hostname);
        if (await dbContext.WorkspaceCustomDomains.IgnoreQueryFilters().AnyAsync(item => item.Hostname == hostname, ct))
            throw new CustomDomainException(409, "That hostname is already registered.");
        var item = new WorkspaceCustomDomain { Id = Guid.NewGuid(), Hostname = hostname, Status = CustomDomainStatuses.Pending, VerificationToken = GenerateToken(), IsEnabled = false, CreatedById = actorId };
        dbContext.WorkspaceCustomDomains.Add(item);
        Audit(item.Id, "custom_domain_created", actorId, new { hostname });
        await SaveAsync(ct);
        return ToDto(item);
    }

    public async Task<CustomDomainDto?> CheckAsync(Guid id, CustomDomainMutationRequest request, Guid? actorId, CancellationToken ct)
    {
        var item = await FindForUpdateAsync(id, request.ConcurrencyStamp, ct); if (item is null) return null;
        item.LastCheckedAt = DateTimeOffset.UtcNow; item.UpdatedById = actorId;
        var expected = VerificationValue(item.VerificationToken);
        try
        {
            var values = await dnsResolver.ResolveAsync(VerificationName(item.Hostname), ct);
            if (values.Contains(expected, StringComparer.Ordinal))
            {
                item.Status = CustomDomainStatuses.Verified; item.VerifiedAt = DateTimeOffset.UtcNow; item.LastFailure = null;
            }
            else
            {
                item.Status = CustomDomainStatuses.Pending; item.IsEnabled = false; item.VerifiedAt = null; item.LastFailure = "The expected TXT value was not found.";
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            item.Status = CustomDomainStatuses.Pending; item.IsEnabled = false; item.LastFailure = "DNS verification could not be completed.";
        }
        Audit(item.Id, "custom_domain_checked", actorId, new { item.Hostname, item.Status });
        await dbContext.SaveChangesAsync(ct); return ToDto(item);
    }

    public Task<CustomDomainDto?> EnableAsync(Guid id, CustomDomainMutationRequest request, Guid? actorId, CancellationToken ct) => MutateAsync(id, request, actorId, true, "custom_domain_enabled", ct);
    public Task<CustomDomainDto?> DisableAsync(Guid id, CustomDomainMutationRequest request, Guid? actorId, CancellationToken ct) => MutateAsync(id, request, actorId, false, "custom_domain_disabled", ct);

    public async Task<CustomDomainDto?> RotateAsync(Guid id, CustomDomainMutationRequest request, Guid? actorId, CancellationToken ct)
    {
        var item = await FindForUpdateAsync(id, request.ConcurrencyStamp, ct); if (item is null) return null;
        item.VerificationToken = GenerateToken(); item.Status = CustomDomainStatuses.Pending; item.IsEnabled = false; item.VerifiedAt = null; item.LastCheckedAt = null; item.LastFailure = null; item.UpdatedById = actorId;
        Audit(item.Id, "custom_domain_challenge_rotated", actorId, new { item.Hostname });
        await dbContext.SaveChangesAsync(ct); return ToDto(item);
    }

    public static string NormalizeHostname(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new CustomDomainException(400, "Hostname is required.");
        var candidate = value.Trim().TrimEnd('.');
        if (candidate.Contains("://", StringComparison.Ordinal) || candidate.Contains('/') || candidate.Contains('*') || candidate.Contains(':')) throw new CustomDomainException(400, "Enter a hostname without a scheme, path, wildcard, or port.");
        string hostname;
        try { hostname = new IdnMapping().GetAscii(candidate).ToLowerInvariant(); }
        catch (ArgumentException) { throw new CustomDomainException(400, "Hostname is not valid."); }
        if (hostname.Length > 253 || Uri.CheckHostName(hostname) != UriHostNameType.Dns || hostname == "localhost" || hostname.EndsWith(".localhost", StringComparison.Ordinal) || IPAddress.TryParse(hostname, out _))
            throw new CustomDomainException(400, "A valid public DNS hostname is required.");
        return hostname;
    }

    private async Task<CustomDomainDto?> MutateAsync(Guid id, CustomDomainMutationRequest request, Guid? actorId, bool enabled, string action, CancellationToken ct)
    {
        var item = await FindForUpdateAsync(id, request.ConcurrencyStamp, ct); if (item is null) return null;
        if (enabled && item.Status != CustomDomainStatuses.Verified) throw new CustomDomainException(400, "Verify the domain before enabling it.");
        item.IsEnabled = enabled; item.UpdatedById = actorId; Audit(item.Id, action, actorId, new { item.Hostname }); await dbContext.SaveChangesAsync(ct); return ToDto(item);
    }
    private async Task<WorkspaceCustomDomain?> FindForUpdateAsync(Guid id, string? stamp, CancellationToken ct)
    {
        var item = await dbContext.WorkspaceCustomDomains.SingleOrDefaultAsync(entry => entry.Id == id, ct); if (item is null) return null;
        if (string.IsNullOrWhiteSpace(stamp) || item.ConcurrencyStamp != stamp.Trim()) throw new DbUpdateConcurrencyException("Custom domain changed. Refresh and try again.");
        return item;
    }
    private async Task SaveAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw new CustomDomainException(409, "That hostname is already registered."); }
    }
    private void Audit(Guid id, string action, Guid? actorId, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "CustomDomain", EntityId = id, Action = action, UserId = actorId, MetadataJson = JsonSerializer.SerializeToDocument(metadata) });
    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    private static string VerificationName(string hostname) => $"_obp-verification.{hostname}";
    private static string VerificationValue(string token) => $"obp-verification={token}";
    private static CustomDomainDto ToDto(WorkspaceCustomDomain item) => new(item.Id, item.Hostname, item.Status, item.IsEnabled, VerificationName(item.Hostname), VerificationValue(item.VerificationToken), item.VerifiedAt, item.LastCheckedAt, item.LastFailure, item.ConcurrencyStamp);
}
