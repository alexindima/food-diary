using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealQuery {
    Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetOverviewReadModelsAsync(
        UserId userId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);
}
