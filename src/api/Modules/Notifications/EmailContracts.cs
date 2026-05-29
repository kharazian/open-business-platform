namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string TextBody,
    string? HtmlBody = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
