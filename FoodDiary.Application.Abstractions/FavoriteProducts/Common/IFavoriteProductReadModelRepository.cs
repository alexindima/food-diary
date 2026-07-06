using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public interface IFavoriteProductReadModelRepository {
    Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
