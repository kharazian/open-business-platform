using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Notifications;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public interface IPasswordRecoveryService
{
    Task RequestResetAsync(RequestPasswordResetRequest request, string? createdIp, CancellationToken cancellationToken);

    Task CompleteResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken);
}

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    private const string GenericResetMessage = "If the email belongs to an active user, a password reset link will be sent.";

    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly LocalPasswordHasher passwordHasher;
    private readonly PasswordResetTokenGenerator tokenGenerator;
    private readonly PasswordResetTokenHasher tokenHasher;
    private readonly IEmailSender emailSender;
    private readonly PasswordRecoveryOptions options;

    public PasswordRecoveryService(
        OpenBusinessPlatformDbContext dbContext,
        LocalPasswordHasher passwordHasher,
        PasswordResetTokenGenerator tokenGenerator,
        PasswordResetTokenHasher tokenHasher,
        IEmailSender emailSender,
        IOptions<PasswordRecoveryOptions> options)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.tokenGenerator = tokenGenerator;
        this.tokenHasher = tokenHasher;
        this.emailSender = emailSender;
        this.options = options.Value;
    }

    public static PasswordResetRequestedResponse CreateGenericResponse()
    {
        return new PasswordResetRequestedResponse(GenericResetMessage);
    }

    public async Task RequestResetAsync(RequestPasswordResetRequest request, string? createdIp, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return;
        }

        var rawToken = tokenGenerator.Generate();
        var tokenHash = tokenHasher.Hash(rawToken);
        var lifetime = GetTokenLifetime();
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedIp = NormalizeCreatedIp(createdIp)
        });
        AddAudit(user.Id, "user_password_reset_requested");

        await dbContext.SaveChangesAsync(cancellationToken);

        var resetLink = PasswordRecoveryEmailFactory.BuildResetLink(options.ResetPasswordUrl, rawToken);
        var message = PasswordRecoveryEmailFactory.CreateResetEmail(user.Email, resetLink, lifetime);
        await emailSender.SendAsync(message, cancellationToken);
    }

    public async Task CompleteResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        ValidatePassword(request.NewPassword);

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Reset token is invalid or expired.");
        }

        var tokenHash = tokenHasher.Hash(request.Token);
        var resetToken = await dbContext.PasswordResetTokens
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null
            || resetToken.UsedAt is not null
            || resetToken.ExpiresAt <= DateTimeOffset.UtcNow
            || resetToken.User is null
            || !resetToken.User.IsActive)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Reset token is invalid or expired.");
        }

        resetToken.User.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        resetToken.User.PasswordUpdatedAt = DateTimeOffset.UtcNow;
        resetToken.UsedAt = DateTimeOffset.UtcNow;
        AddAudit(resetToken.UserId, "user_password_reset_completed");

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan GetTokenLifetime()
    {
        var minutes = options.TokenLifetimeMinutes <= 0 ? 60 : options.TokenLifetimeMinutes;
        return TimeSpan.FromMinutes(minutes);
    }

    private static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Email is required.");
        }

        return value.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Password must be at least 8 characters.");
        }
    }

    private static string? NormalizeCreatedIp(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void AddAudit(Guid userId, string action)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            EntityType = "User",
            EntityId = userId,
            Action = action
        });
    }
}

public sealed class PasswordResetTokenGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed class PasswordResetTokenHasher
{
    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string token, string expectedHash)
    {
        var actualHash = Hash(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHash),
            Encoding.UTF8.GetBytes(expectedHash));
    }
}

public static class PasswordRecoveryEmailFactory
{
    public static string BuildResetLink(string resetPasswordUrl, string token)
    {
        var separator = resetPasswordUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{resetPasswordUrl}{separator}token={Uri.EscapeDataString(token)}";
    }

    public static EmailMessage CreateResetEmail(string toEmail, string resetLink, TimeSpan tokenLifetime)
    {
        var lifetimeMinutes = Math.Max(1, (int)Math.Ceiling(tokenLifetime.TotalMinutes));
        var textBody = $"""
            We received a request to reset your Open Business Platform password.

            Use this link to choose a new password:
            {resetLink}

            This link expires in {lifetimeMinutes} minutes. If you did not request a password reset, you can ignore this email.
            """;

        return new EmailMessage(
            toEmail,
            "Reset your Open Business Platform password",
            textBody);
    }
}
