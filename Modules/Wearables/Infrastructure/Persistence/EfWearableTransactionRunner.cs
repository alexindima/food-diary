using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Modules.Wearables.Infrastructure.Persistence;

internal sealed class EfWearableTransactionRunner(IModuleSessionCoordinator coordinator) : IWearableTransactionRunner {
    public Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializedAsync(serializationKey, operation, cancellationToken);
}
