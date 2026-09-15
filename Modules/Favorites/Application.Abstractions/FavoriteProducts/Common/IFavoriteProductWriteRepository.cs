using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductWriteRepository {
    Task<FavoriteProduct?> GetByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteProduct?> GetOwnedByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, userId, asTracking, cancellationToken);

    Task<FavoriteProduct?> GetByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

    Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);
}
