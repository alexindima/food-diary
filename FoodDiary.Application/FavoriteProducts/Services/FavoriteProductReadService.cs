using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Services;

public sealed class FavoriteProductReadService(IFavoriteProductReadRepository favoriteProductRepository)
    : IFavoriteProductReadService {
    public async Task<IReadOnlyList<FavoriteProductModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProduct> favorites = await favoriteProductRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

    public Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        favoriteProductRepository.ExistsByProductIdAsync(productId, userId, cancellationToken);
}
