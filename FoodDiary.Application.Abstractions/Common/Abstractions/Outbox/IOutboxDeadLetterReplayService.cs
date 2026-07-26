namespace FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;

public interface IOutboxDeadLetterReplayService {
    Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListDeadLettersAsync(
        string? outboxName,
        int limit,
        CancellationToken cancellationToken = default);

    Task<OutboxDeadLetterMessageModel?> GetDeadLetterAsync(
        string outboxName,
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxReplayAuditModel>> ListReplayHistoryAsync(
        string? outboxName,
        Guid? messageId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<OutboxReplayAuditModel> ReplayAsync(
        string outboxName,
        Guid messageId,
        string requestedBy,
        string reason,
        int expectedAttemptCount,
        CancellationToken cancellationToken = default);
}
