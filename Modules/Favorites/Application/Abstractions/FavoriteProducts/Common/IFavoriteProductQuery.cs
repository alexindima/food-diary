using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductQuery {
    Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteProductId>> GetAccessibleIdsAsync(
        UserId userId,
        FavoriteProductId? favoriteId = null,
        IReadOnlyCollection<ProductId>? sourceIds = null,
        CancellationToken cancellationToken = default);
}
