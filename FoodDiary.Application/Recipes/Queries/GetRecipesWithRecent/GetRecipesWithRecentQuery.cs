using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;

public sealed record GetRecipesWithRecentQuery(
    UserId? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10)
    : IQuery<Result<RecipeListWithRecentResponse>>;
