using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadMealFavoriteStatusQuery(MealId MealId, UserId UserId) : IQuery<bool>;
