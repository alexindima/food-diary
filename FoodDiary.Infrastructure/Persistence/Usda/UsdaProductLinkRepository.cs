using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Usda;

internal sealed class UsdaProductLinkRepository(IProductRepository productRepository) : IUsdaProductLinkRepository {
    public Task<Product?> GetForLinkUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        productRepository.GetByIdForUpdateAsync(productId, userId, includePublic: false, cancellationToken);

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) =>
        productRepository.UpdateAsync(product, cancellationToken);
}
