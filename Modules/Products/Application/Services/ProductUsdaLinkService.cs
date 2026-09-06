using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Services;

public sealed class ProductUsdaLinkService(IProductWriteRepository productRepository) : IUsdaProductLinkService {
    public async Task<Result> IsAccessibleForUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false) is not null
            ? Result.Success() : Result.Failure(ProductErrors.NotAccessible(productId.Value));

    public async Task<Result> LinkAsync(
        ProductId productId,
        UserId userId,
        int fdcId,
        CancellationToken cancellationToken = default) {
        Product? product = await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(ProductErrors.NotAccessible(productId.Value));
        }

        product.LinkToUsdaFood(fdcId);
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> UnlinkAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        Product? product = await GetProductAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(ProductErrors.NotAccessible(productId.Value));
        }

        product.UnlinkUsdaFood();
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        return Result.Success();
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
