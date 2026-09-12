using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Products;

public sealed class ProductSnapshotReadService(FoodDiaryDbContext context) : IProductSnapshotReadService {
    public async Task<IReadOnlyDictionary<ProductId, ProductSnapshotReadModel>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (productIds.Count == 0) { return new Dictionary<ProductId, ProductSnapshotReadModel>(); }
        ProductId[] ids = [.. productIds.Distinct()];
        return await context.Products.AsNoTracking().Where(product => Enumerable.Contains(ids, product.Id))
            .Select(product => new ProductSnapshotReadModel(product.Id, product.Name, product.ImageUrl,
                product.BaseUnit, product.BaseAmount, product.CaloriesPerBase, product.ProteinsPerBase,
                product.FatsPerBase, product.CarbsPerBase, product.FiberPerBase, product.AlcoholPerBase,
                product.ProductType, product.Visibility, product.Category))
            .ToDictionaryAsync(product => product.Id, cancellationToken).ConfigureAwait(false);
    }
}
