using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Commands.DuplicateRecipe;

public sealed record DuplicateRecipeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeModel>>, IUserRequest;
