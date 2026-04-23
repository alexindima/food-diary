using System.Net.Mail;

namespace FoodDiary.Infrastructure.Services;

public interface IEmailTransport {
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}
