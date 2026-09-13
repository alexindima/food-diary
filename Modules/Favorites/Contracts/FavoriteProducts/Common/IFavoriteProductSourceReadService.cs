using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductSourceReadService {
    Task<Result<FavoriteProductSourceModel>> GetAccessibleAsync(ProductId id, UserId userId, CancellationToken cancellationToken = default);
}
