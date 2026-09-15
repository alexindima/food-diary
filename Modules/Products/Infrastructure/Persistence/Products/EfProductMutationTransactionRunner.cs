using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence.Products;

internal sealed class EfProductMutationTransactionRunner(IModuleTransactionCoordinator coordinator) : IProductMutationTransactionRunner {
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteSerializableAsync(operation, cancellationToken);
}
