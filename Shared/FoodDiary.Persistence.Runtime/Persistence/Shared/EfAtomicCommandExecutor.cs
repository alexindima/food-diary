using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class EfAtomicCommandExecutor(IModuleTransactionCoordinator coordinator) : IAtomicCommandExecutor {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteAsync((_, token) => operation(token), cancellationToken);
}
