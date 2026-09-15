using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadMealFavoriteIdsQuery(UserId UserId, IReadOnlyCollection<MealId> MealIds) : IQuery<IReadOnlyDictionary<MealId, FavoriteMealId>>;
