using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence.Products;

public sealed class ProductSnapshotReadService(DbSet<Product> records, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IProductSnapshotReadService {
    public async Task<IReadOnlyDictionary<ProductId, ProductSnapshotReadModel>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (productIds.Count == 0) { return new Dictionary<ProductId, ProductSnapshotReadModel>(); }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        ProductId[] ids = [.. productIds.Distinct()];
        return await records.AsNoTracking().Where(product => Enumerable.Contains(ids, product.Id))
            .Select(product => new ProductSnapshotReadModel(product.Id, product.Name, product.ImageUrl,
                product.BaseUnit, product.BaseAmount, product.CaloriesPerBase, product.ProteinsPerBase,
                product.FatsPerBase, product.CarbsPerBase, product.FiberPerBase, product.AlcoholPerBase,
                product.ProductType, product.Visibility, product.Category))
            .ToDictionaryAsync(product => product.Id, cancellationToken).ConfigureAwait(false);
    }
}
