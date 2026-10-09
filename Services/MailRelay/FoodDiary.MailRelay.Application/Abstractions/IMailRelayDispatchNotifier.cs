namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayDispatchNotifier {
    Task NotifyQueuedAsync(QueuedEmailId queuedEmailId, CancellationToken cancellationToken);
}
