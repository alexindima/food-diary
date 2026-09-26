namespace FoodDiary.Email.Contracts.Email.Common;

public interface IEmailOutbox {
    Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
