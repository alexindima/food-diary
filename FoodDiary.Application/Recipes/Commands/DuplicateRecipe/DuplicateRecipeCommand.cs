using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    UserId? UserId,
    RecipeId RecipeId) : ICommand<Result<RecipeModel>>;
