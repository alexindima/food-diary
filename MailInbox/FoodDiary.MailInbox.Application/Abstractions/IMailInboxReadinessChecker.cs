namespace FoodDiary.MailInbox.Application.Abstractions;

public interface IMailInboxReadinessChecker {
    Task CheckReadyAsync(CancellationToken cancellationToken);
}
