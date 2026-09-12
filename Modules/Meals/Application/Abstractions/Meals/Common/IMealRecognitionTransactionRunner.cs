using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealRecognitionTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(UserId userId, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    // Flush within the open transaction to capture the persisted optimistic concurrency version.
    Task<uint> FlushCreatedMealAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default);
}
