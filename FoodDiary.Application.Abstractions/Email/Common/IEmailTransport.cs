namespace FoodDiary.Application.Abstractions.Email.Common;

public interface IEmailTransport {
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
