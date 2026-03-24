namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecipesHttpQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    bool IncludePublic = true);
