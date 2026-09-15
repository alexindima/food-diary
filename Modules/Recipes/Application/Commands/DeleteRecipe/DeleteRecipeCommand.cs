using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Commands.DeleteRecipe;

public record DeleteRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result>, IUserRequest;
