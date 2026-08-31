using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Common;

public interface IMealReadService : IFavoriteMealSourceReadService {
    Task<PagedResponse<MealModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken);

    Task<MealOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters,
        CancellationToken cancellationToken);

    Task<MealModel?> GetByIdAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken);
}
