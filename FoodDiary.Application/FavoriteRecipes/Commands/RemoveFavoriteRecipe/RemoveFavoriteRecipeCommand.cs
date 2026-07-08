using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;

public record RemoveFavoriteRecipeCommand(
    Guid? UserId,
    Guid FavoriteRecipeId) : ICommand<Result>, IUserRequest;
