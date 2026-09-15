using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipes;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadFavoriteRecipesQuery(UserId UserId) : IQuery<IReadOnlyList<FavoriteRecipeModel>>;
