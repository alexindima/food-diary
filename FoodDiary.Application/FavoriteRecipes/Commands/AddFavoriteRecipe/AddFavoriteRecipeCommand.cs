using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.FavoriteRecipes.Models;

namespace FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;

public record AddFavoriteRecipeCommand(
    Guid? UserId,
    Guid RecipeId,
    string? Name) : ICommand<Result<FavoriteRecipeModel>>, IUserRequest;
