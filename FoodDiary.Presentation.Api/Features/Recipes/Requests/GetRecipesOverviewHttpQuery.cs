namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecipesOverviewHttpQuery(
    int Page = 1,
    int Limit = 10,
    int RecentLimit = 10,
    int FavoriteLimit = 10,
    string? Search = null,
    bool IncludePublic = true,
    string? Category = null,
    int? MaxTotalTime = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
