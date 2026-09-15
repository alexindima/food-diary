using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductReadModelRepository {
    Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
