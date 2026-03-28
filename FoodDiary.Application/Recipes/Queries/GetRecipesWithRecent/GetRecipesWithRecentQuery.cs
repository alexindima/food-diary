using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;

public sealed record GetRecipesWithRecentQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10)
    : IQuery<Result<RecipeListWithRecentModel>>, IUserRequest;
