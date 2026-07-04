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

    Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default);
}
