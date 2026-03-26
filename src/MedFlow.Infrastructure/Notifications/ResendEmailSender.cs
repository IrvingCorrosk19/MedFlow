using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using Microsoft.Extensions.Options;
using Resend;

namespace MedFlow.Infrastructure.Notifications;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;

    public ResendEmailSender(IResend resend, IOptions<ResendOptions> options)
    {
        _resend = resend;
        _options = options.Value;
    }

    public async Task<EmailSendResult> SendAsync(string to, string? subject, string? htmlBody, string? textBody, string? fromEmail = null, string? fromName = null, string? replyTo = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return new EmailSendResult(false, null, "Resend API key not configured.");

        try
        {
            var from = fromEmail ?? _options.FromEmail;
            if (!string.IsNullOrWhiteSpace(fromName))
                from = $"{fromName} <{from}>";

            var message = new EmailMessage
            {
                From = from,
                Subject = subject ?? "(Sin asunto)",
                HtmlBody = htmlBody ?? textBody ?? ""
            };
            message.To.Add(to);
            if (!string.IsNullOrWhiteSpace(replyTo))
                message.ReplyTo = replyTo;

            var response = await _resend.EmailSendAsync(message, cancellationToken);
            if (response?.Success == true)
                return new EmailSendResult(true, response.Content.ToString(), null);
            return new EmailSendResult(false, null, response?.Exception?.Message ?? "Unknown error");
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, null, ex.Message);
        }
    }
}
