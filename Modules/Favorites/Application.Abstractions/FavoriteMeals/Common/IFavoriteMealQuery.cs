using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealQuery {
    Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);
}
