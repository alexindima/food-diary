namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record ExploreRecipesHttpQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    string? Category = null,
    int? MaxPrepTime = null,
    string SortBy = "newest");
