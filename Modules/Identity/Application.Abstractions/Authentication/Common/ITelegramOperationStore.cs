using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface ITelegramOperationStore {
    Task<TelegramOperationId?> RegisterAsync(long botId, long updateId, UserId userId, long securityVersion, string payload, CancellationToken cancellationToken);
    Task<IReadOnlyList<TelegramOperationId>> ListReadyAsync(long botId, CancellationToken cancellationToken);
    Task<TelegramOperationLease?> AcquireAsync(long botId, TelegramOperationId operationId, CancellationToken cancellationToken);
    Task<TelegramOperationLease?> GetLeaseAsync(long botId, TelegramOperationId operationId, TelegramLeaseId leaseId, CancellationToken cancellationToken);
    Task<bool> CheckpointAsync(long botId, TelegramOperationId operationId, TelegramLeaseId leaseId, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken);
    Task CancelUserAsync(UserId userId, CancellationToken cancellationToken);
    Task CancelOperationAsync(long botId, TelegramOperationId operationId, CancellationToken cancellationToken);
}
