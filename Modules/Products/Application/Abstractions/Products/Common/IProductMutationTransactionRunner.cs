namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductMutationTransactionRunner {
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
