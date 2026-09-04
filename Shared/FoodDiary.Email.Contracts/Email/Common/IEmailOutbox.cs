namespace FoodDiary.Application.Abstractions.Email.Common;

public interface IEmailOutbox {
    Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
