using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;

public record AddFavoriteRecipeCommand(
    Guid? UserId,
    Guid RecipeId,
    string? Name) : ICommand<Result<FavoriteRecipeModel>>, IUserRequest;
