using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Services;

public sealed class FavoriteProductReadService(
    IFavoriteProductReadModelRepository favoriteProductReadModelRepository,
    IFavoriteProductReadRepository favoriteProductRepository)
    : IFavoriteProductReadService {
    public async Task<IReadOnlyList<FavoriteProductModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProductReadModel> favorites = await favoriteProductReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

    public Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        favoriteProductRepository.ExistsByProductIdAsync(productId, userId, cancellationToken);
}
