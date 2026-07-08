using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public record DeleteRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result>, IUserRequest;
