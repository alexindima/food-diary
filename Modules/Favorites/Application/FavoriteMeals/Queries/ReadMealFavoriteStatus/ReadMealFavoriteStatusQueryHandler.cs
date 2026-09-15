using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteStatus;

public sealed class ReadMealFavoriteStatusQueryHandler(IFavoriteMealReadModelRepository favoriteMealReadModelRepository) : IQueryHandler<ReadMealFavoriteStatusQuery, bool> {
    public Task<bool> Handle(ReadMealFavoriteStatusQuery request, CancellationToken cancellationToken) {
        MealId mealId = request.MealId;
        UserId userId = request.UserId;
        return favoriteMealReadModelRepository.ExistsByMealIdAsync(mealId, userId, cancellationToken);
    }

}
