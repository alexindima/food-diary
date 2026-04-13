namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetProductsOverviewHttpQuery(
    int Page = 1,
    int Limit = 10,
    int RecentLimit = 10,
    int FavoriteLimit = 10,
    string? Search = null,
    bool IncludePublic = true,
    string? ProductTypes = null);
