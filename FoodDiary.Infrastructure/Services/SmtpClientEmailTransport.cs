using System.Net;
using System.Net.Mail;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

internal sealed class SmtpClientEmailTransport(IOptions<EmailOptions> options) : IEmailTransport {
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost)) {
            throw new InvalidOperationException("Email SMTP host is not configured.");
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort) {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
