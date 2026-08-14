using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Recipes.Models;

namespace FoodDiary.Application.Recipes.Recipes.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeModel>>, IUserRequest;
