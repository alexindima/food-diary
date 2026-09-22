using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealQuery {
    Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetPageReadModelsAsync(
        UserId userId, int page, int limit, string? search, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetOverviewReadModelsAsync(
        UserId userId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);
}
