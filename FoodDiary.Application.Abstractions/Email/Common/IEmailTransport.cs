using System.Net.Mail;

namespace FoodDiary.Application.Abstractions.Email.Common;

public interface IEmailTransport {
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}
