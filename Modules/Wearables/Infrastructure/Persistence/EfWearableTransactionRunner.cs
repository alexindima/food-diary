using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class EfWearableTransactionRunner(IModuleSessionCoordinator coordinator) : IWearableTransactionRunner {
    public Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializedAsync(serializationKey, operation, cancellationToken);
}
