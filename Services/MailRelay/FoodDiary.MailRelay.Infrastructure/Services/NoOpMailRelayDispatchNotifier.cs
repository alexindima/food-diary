namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class NoOpMailRelayDispatchNotifier : IMailRelayDispatchNotifier {
    public Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) => Task.CompletedTask;
}
