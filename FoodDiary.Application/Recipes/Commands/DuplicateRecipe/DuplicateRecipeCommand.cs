using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeModel>>, IUserRequest;
