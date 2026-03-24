namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecipesWithRecentHttpQuery(
    int Page = 1,
    int Limit = 10,
    int RecentLimit = 10,
    string? Search = null,
    bool IncludePublic = true);
