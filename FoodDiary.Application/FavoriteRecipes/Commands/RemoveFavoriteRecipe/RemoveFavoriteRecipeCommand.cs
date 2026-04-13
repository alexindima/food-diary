using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;

public record RemoveFavoriteRecipeCommand(
    Guid? UserId,
    Guid FavoriteRecipeId) : ICommand<Result>, IUserRequest;
