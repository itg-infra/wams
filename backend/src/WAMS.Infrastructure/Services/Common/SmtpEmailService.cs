namespace WAMS.Infrastructure.Services.Common;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Common;

public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        using var client = await ConnectAsync(ct);
        await client.SendAsync(BuildMimeMessage(message), ct);
        await client.DisconnectAsync(true, ct);
        logger.LogInformation("[Email] Sent to {To} subject: {Subject}", message.To, message.Subject);
    }

    public async Task SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default)
    {
        using var client = await ConnectAsync(ct);

        foreach (var message in messages)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await client.SendAsync(BuildMimeMessage(message), ct);
                logger.LogInformation("[Email] Sent to {To} subject: {Subject}", message.To, message.Subject);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[Email] Failed to send to {To}", message.To);
            }
        }

        await client.DisconnectAsync(true, ct);
    }

    private async Task<SmtpClient> ConnectAsync(CancellationToken ct)
    {
        var client = new SmtpClient();
        var socketOptions = _opts.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
        await client.ConnectAsync(_opts.Host, _opts.Port, socketOptions, ct);
        if (!string.IsNullOrEmpty(_opts.Username))
            await client.AuthenticateAsync(_opts.Username, _opts.Password, ct);
        return client;
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("html") { Text = message.HtmlBody };
        return mime;
    }
}
