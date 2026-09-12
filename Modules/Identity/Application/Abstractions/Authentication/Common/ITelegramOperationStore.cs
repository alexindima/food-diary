namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface ITelegramOperationStore {
    Task<Guid?> RegisterAsync(long botId, long updateId, Guid userId, long securityVersion, string payload, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListReadyAsync(long botId, CancellationToken cancellationToken);
    Task<TelegramOperationLease?> AcquireAsync(long botId, Guid operationId, CancellationToken cancellationToken);
    Task<TelegramOperationLease?> GetLeaseAsync(long botId, Guid operationId, Guid leaseId, CancellationToken cancellationToken);
    Task<bool> CheckpointAsync(long botId, Guid operationId, Guid leaseId, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken);
    Task CancelUserAsync(Guid userId, CancellationToken cancellationToken);
    Task CancelOperationAsync(long botId, Guid operationId, CancellationToken cancellationToken);
}
