using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Modules.Recipes.Infrastructure.Persistence.Recipes;

internal sealed class EfRecipeMutationTransactionRunner(IModuleTransactionCoordinator coordinator) : IRecipeMutationTransactionRunner {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializableAsync(operation, cancellationToken);
}
