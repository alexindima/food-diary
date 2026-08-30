using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Favorites.FavoriteRecipes.Queries.IsRecipeFavorite;

public record IsRecipeFavoriteQuery(
    Guid? UserId,
    Guid RecipeId) : IQuery<Result<bool>>, IUserRequest;
