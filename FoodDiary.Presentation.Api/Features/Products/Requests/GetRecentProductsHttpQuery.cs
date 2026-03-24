namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetRecentProductsHttpQuery(
    int Limit = 10,
    bool IncludePublic = true);
