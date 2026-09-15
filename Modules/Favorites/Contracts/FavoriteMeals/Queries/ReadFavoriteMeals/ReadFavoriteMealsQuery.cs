using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadFavoriteMealsQuery(UserId UserId) : IQuery<IReadOnlyList<FavoriteMealModel>>;
