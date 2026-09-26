namespace FoodDiary.Email.Contracts.Email.Common;

public interface IEmailTransport {
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
