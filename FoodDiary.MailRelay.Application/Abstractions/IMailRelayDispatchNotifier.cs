namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayDispatchNotifier {
    Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken);
}
