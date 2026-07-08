using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeModel>>, IUserRequest;
