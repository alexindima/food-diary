namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecentRecipesHttpQuery(
    int Limit = 10,
    bool IncludePublic = true);
