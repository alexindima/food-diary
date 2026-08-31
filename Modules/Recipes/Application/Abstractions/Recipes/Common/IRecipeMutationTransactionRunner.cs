namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeMutationTransactionRunner {
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
