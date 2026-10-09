namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class NoOpMailRelayDispatchNotifier : IMailRelayDispatchNotifier {
    public Task NotifyQueuedAsync(QueuedEmailId queuedEmailId, CancellationToken cancellationToken) => Task.CompletedTask;
}
