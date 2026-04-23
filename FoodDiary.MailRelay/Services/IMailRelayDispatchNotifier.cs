namespace FoodDiary.MailRelay.Services;

public interface IMailRelayDispatchNotifier {
    Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken);
}
