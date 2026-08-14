using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Recipes.Models;

namespace FoodDiary.Application.Recipes.Recipes.Queries.GetRecipeById;

public record GetRecipeByIdQuery(
    Guid? UserId,
    Guid RecipeId,
    bool IncludePublic) : IQuery<Result<RecipeModel>>, IUserRequest;
