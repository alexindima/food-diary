using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadRecipeFavoriteStatusQuery(RecipeId RecipeId, UserId UserId) : IQuery<bool>;
