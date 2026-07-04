using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductWriteRepository : IFavoriteProductReadRepository {
    Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

    Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);
}
