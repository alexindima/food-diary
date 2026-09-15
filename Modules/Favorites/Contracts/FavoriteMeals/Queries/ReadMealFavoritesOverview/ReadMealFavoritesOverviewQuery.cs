using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadMealFavoritesOverviewQuery(UserId UserId, int Limit) : IQuery<(IReadOnlyList<MealFavoriteMealModel> Items, int TotalItems)>;
