namespace WAMS.Infrastructure.Services.Common;

using Microsoft.Extensions.Logging;
using WAMS.Application.Interfaces.Common;

public class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogDebug("[Email] Disabled - skipping send to {To} subject: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }

    public Task SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default)
    {
        foreach (var msg in messages)
            logger.LogDebug("[Email] Disabled - skipping send to {To} subject: {Subject}", msg.To, msg.Subject);
        return Task.CompletedTask;
    }
}
