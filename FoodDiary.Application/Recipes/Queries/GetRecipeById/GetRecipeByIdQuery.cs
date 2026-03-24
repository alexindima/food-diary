using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public record GetRecipeByIdQuery(
    Guid? UserId,
    RecipeId RecipeId,
    bool IncludePublic) : IQuery<Result<RecipeModel>>;
