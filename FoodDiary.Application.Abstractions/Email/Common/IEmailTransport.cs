using System.Net.Mail;

namespace FoodDiary.Application.Email.Common;

public interface IEmailTransport {
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}
