using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductReadService {
    Task<IReadOnlyList<FavoriteProductModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
