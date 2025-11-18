using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public record DeleteRecipeCommand(
    UserId? UserId,
    RecipeId RecipeId) : ICommand<Result<bool>>;
