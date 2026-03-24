using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(
    UserId? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic) : IQuery<Result<PagedResponse<RecipeModel>>>;
