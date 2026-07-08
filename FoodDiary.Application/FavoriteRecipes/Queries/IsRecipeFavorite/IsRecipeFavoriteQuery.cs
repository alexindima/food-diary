using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;

public record IsRecipeFavoriteQuery(
    Guid? UserId,
    Guid RecipeId) : IQuery<Result<bool>>, IUserRequest;
