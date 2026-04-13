using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed record GetRecipesOverviewQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10,
    int FavoriteLimit = 10)
    : IQuery<Result<RecipeOverviewModel>>, IUserRequest;
