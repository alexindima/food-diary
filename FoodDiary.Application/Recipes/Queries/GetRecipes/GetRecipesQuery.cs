using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(
    UserId? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic) : IQuery<Result<PagedResponse<RecipeResponse>>>;
