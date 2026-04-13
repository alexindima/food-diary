using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Common;

public interface IFavoriteProductRepository {
    Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

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
}
