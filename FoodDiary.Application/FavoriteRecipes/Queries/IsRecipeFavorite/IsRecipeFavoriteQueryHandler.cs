using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;

public class IsRecipeFavoriteQueryHandler(
    IFavoriteRecipeRepository favoriteRecipeRepository,
    IUserRepository userRepository)
    : IQueryHandler<IsRecipeFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsRecipeFavoriteQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<bool>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<bool>(accessError);
        }

        var recipeId = new RecipeId(query.RecipeId);
        var favorite = await favoriteRecipeRepository.GetByRecipeIdAsync(recipeId, userId, cancellationToken);
        return Result.Success(favorite is not null);
    }
}
