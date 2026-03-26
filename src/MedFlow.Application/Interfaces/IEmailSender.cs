namespace MedFlow.Application.Interfaces;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(string to, string? subject, string? htmlBody, string? textBody, string? fromEmail = null, string? fromName = null, string? replyTo = null, CancellationToken cancellationToken = default);
}

public sealed record EmailSendResult(bool Success, string? ExternalId, string? ErrorMessage);
