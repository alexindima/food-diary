using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteIds;

public sealed class ReadMealFavoriteIdsQueryHandler(IFavoriteMealReadModelRepository favoriteMealReadModelRepository) : IQueryHandler<ReadMealFavoriteIdsQuery, IReadOnlyDictionary<MealId, FavoriteMealId>> {
    public Task<IReadOnlyDictionary<MealId, FavoriteMealId>> Handle(ReadMealFavoriteIdsQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        IReadOnlyCollection<MealId> mealIds = request.MealIds;
        return favoriteMealReadModelRepository.GetFavoriteIdsByMealIdsAsync(userId, mealIds, cancellationToken);
    }

}
