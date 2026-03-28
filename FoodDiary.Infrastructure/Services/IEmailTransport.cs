using System.Net.Mail;

namespace FoodDiary.Infrastructure.Services;

public interface IEmailTransport {
    Task SendAsync(
        MailMessage message,
        string host,
        int port,
        bool useSsl,
        string? username,
        string? password,
        CancellationToken cancellationToken);
}
