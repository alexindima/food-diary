namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

public interface IProductMutationTransactionRunner {
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
