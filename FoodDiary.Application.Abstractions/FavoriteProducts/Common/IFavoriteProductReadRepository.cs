using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductReadRepository {
    Task<FavoriteProduct?> GetByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteProduct?> GetByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        await GetByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false) is not null;

    Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProduct> favorites = await GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(static favorite => new FavoriteProductReadModel(
            favorite.Id.Value,
            favorite.ProductId.Value,
            favorite.UserId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Product.Name,
            favorite.Product.Brand,
            favorite.Product.Barcode,
            favorite.Product.UserId == favorite.UserId ? favorite.Product.Comment : null,
            favorite.Product.ImageUrl,
            favorite.Product.CaloriesPerBase,
            favorite.Product.ProteinsPerBase,
            favorite.Product.FatsPerBase,
            favorite.Product.CarbsPerBase,
            favorite.Product.FiberPerBase,
            favorite.Product.AlcoholPerBase,
            favorite.Product.ProductType,
            favorite.Product.BaseUnit,
            favorite.PreferredPortionAmount,
            favorite.Product.DefaultPortionAmount,
            favorite.Product.UserId.Value))];
    }

    Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default);
}
