namespace WAMS.Application.Interfaces.Common;

public record EmailMessage(
    string To,
    string ToName,
    string Subject,
    string HtmlBody
);

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
    Task SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default);
}
