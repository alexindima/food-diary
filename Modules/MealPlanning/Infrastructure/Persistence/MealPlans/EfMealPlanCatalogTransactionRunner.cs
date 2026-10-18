using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans;

internal sealed class EfMealPlanCatalogTransactionRunner(IModuleTransactionCoordinator coordinator) : IMealPlanCatalogTransactionRunner {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializableAsync(operation, cancellationToken);
}
