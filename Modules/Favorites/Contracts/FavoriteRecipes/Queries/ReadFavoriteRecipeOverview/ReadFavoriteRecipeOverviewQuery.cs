using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;

// Trusted owner operation: callers authorize the user and supplied recipe scope.
public sealed record ReadFavoriteRecipeOverviewQuery(UserId UserId, IReadOnlyCollection<RecipeId> RecipeIds, int PreviewLimit = 0)
    : IQuery<FavoriteRecipeOverviewModel>;
