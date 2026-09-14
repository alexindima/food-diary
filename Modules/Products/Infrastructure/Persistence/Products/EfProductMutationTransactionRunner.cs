using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Infrastructure.Persistence.Products;

internal sealed class EfProductMutationTransactionRunner(IModuleTransactionCoordinator coordinator) : IProductMutationTransactionRunner {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializableAsync(operation, cancellationToken);
}
