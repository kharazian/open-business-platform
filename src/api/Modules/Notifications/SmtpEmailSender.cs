using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Configuration;

namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            var attachments = message.Attachments ?? Array.Empty<EmailAttachment>();
            logger.LogInformation(
                "Email is not configured. Development email to {ToEmail}: {Subject}{LineBreak}{Body}{LineBreak}Attachments: {Attachments}",
                message.ToEmail,
                message.Subject,
                Environment.NewLine,
                message.TextBody,
                Environment.NewLine,
                attachments.Count == 0
                    ? "none"
                    : string.Join(", ", attachments.Select(attachment => $"{attachment.FileName} ({attachment.ContentType}, {attachment.Content.Length} bytes)")));
            return;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody ?? message.TextBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };
        mailMessage.To.Add(message.ToEmail);

        foreach (var attachment in message.Attachments ?? Array.Empty<EmailAttachment>())
        {
            mailMessage.Attachments.Add(new Attachment(
                new MemoryStream(attachment.Content),
                attachment.FileName,
                attachment.ContentType));
        }

        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.UseStartTls
        };

        if (!string.IsNullOrWhiteSpace(options.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(options.SmtpUsername, options.SmtpPassword);
        }

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
