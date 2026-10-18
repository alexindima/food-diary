namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanCatalogTransactionRunner {
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
