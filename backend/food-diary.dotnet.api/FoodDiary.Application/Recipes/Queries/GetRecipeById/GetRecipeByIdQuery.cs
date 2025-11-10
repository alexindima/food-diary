using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public record GetRecipeByIdQuery(
    UserId? UserId,
    RecipeId RecipeId,
    bool IncludePublic) : IQuery<Result<RecipeResponse>>;
