using System.Net;
using System.Net.Mail;

namespace FoodDiary.Infrastructure.Services;

internal sealed class SmtpClientEmailTransport : IEmailTransport {
    public async Task SendAsync(
        MailMessage message,
        string host,
        int port,
        bool useSsl,
        string? username,
        string? password,
        CancellationToken cancellationToken) {
        using var client = new SmtpClient(host, port) {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
