namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string TextBody,
    string? HtmlBody = null,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
