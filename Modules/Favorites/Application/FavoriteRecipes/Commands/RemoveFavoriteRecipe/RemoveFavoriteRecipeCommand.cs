using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Favorites.FavoriteRecipes.Commands.RemoveFavoriteRecipe;

public record RemoveFavoriteRecipeCommand(
    Guid? UserId,
    Guid FavoriteRecipeId) : ICommand<Result>, IUserRequest;
