namespace FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;

public interface IOutboxDeadLetterReplayService {
    Task ReplayAsync(
        string outboxName,
        Guid messageId,
        string requestedBy,
        string reason,
        CancellationToken cancellationToken = default);
}
