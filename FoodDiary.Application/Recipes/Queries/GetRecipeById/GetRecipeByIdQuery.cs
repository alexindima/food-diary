using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public record GetRecipeByIdQuery(
    Guid? UserId,
    Guid RecipeId,
    bool IncludePublic) : IQuery<Result<RecipeModel>>, IUserRequest;
