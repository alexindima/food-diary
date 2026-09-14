using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

internal sealed class EfRecipeMutationTransactionRunner(IModuleTransactionCoordinator coordinator) : IRecipeMutationTransactionRunner {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializableAsync(operation, cancellationToken);
}
