namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetProductsHttpQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    bool IncludePublic = true,
    string? ProductTypes = null);
