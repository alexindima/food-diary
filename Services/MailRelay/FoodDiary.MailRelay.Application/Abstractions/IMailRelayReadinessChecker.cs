namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayReadinessChecker {
    Task CheckReadyAsync(CancellationToken cancellationToken);
}
