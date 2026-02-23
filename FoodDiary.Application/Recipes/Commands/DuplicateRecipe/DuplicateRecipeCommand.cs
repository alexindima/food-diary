using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    UserId? UserId,
    RecipeId RecipeId) : ICommand<Result<RecipeResponse>>;
