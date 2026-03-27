using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public record DeleteRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result>;
