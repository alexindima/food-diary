using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Services;

public sealed class ProductUsdaLinkService(IProductWriteRepository productRepository) : IProductUsdaLinkService {
    public async Task<bool> IsAccessibleForUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false) is not null;

    public async Task<bool> LinkAsync(
        ProductId productId,
        UserId userId,
        int fdcId,
        CancellationToken cancellationToken = default) {
        Product? product = await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return false;
        }

        product.LinkToUsdaFood(fdcId);
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> UnlinkAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        Product? product = await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return false;
        }

        product.UnlinkUsdaFood();
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private Task<Product?> GetProductAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken) =>
        productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken);
}
