using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;

namespace FoodDiary.Application.Favorites.FavoriteRecipes.Commands.AddFavoriteRecipe;

public record AddFavoriteRecipeCommand(
    Guid? UserId,
    Guid RecipeId,
    string? Name) : ICommand<Result<FavoriteRecipeModel>>, IUserRequest;
