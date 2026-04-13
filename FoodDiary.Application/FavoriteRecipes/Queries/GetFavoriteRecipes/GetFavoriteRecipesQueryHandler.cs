using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;

public class GetFavoriteRecipesQueryHandler(
    IFavoriteRecipeRepository favoriteRecipeRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetFavoriteRecipesQuery, Result<IReadOnlyList<FavoriteRecipeModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteRecipeModel>>> Handle(
        GetFavoriteRecipesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteRecipeModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteRecipeModel>>(accessError);
        }

        var favorites = await favoriteRecipeRepository.GetAllAsync(userId, cancellationToken);
        var models = favorites.Select(f => f.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FavoriteRecipeModel>>(models);
    }
}
