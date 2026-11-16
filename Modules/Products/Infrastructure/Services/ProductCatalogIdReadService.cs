using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Services;

internal sealed class ProductCatalogIdReadService(ProductsDbContext context, Func<CancellationToken, Task> synchronizeTransactionAsync)
    : IProductCatalogIdReadService {
    public async Task<bool?> CatalogIdIsPublicAsync(Guid id, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken);
        return await context.Products.AsNoTracking().Where(item => item.Id == new ProductId(id))
            .Select(item => (bool?)(item.Visibility == Visibility.Public)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool?> FindUsdaCatalogMatchAsync(Guid id, int fdcId, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken);
        var productId = new ProductId(id);
        return await context.Products.AsNoTracking()
            .Where(item => item.Id == productId || (item.Visibility == Visibility.Public && item.UsdaFdcId == fdcId))
            .OrderByDescending(item => item.Id == productId)
            .Select(item => (bool?)(item.Visibility == Visibility.Public && item.UsdaFdcId == fdcId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
