using FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Products.Responses;

public sealed record ProductOverviewHttpResponse(
    IReadOnlyList<ProductHttpResponse> RecentItems,
    PagedHttpResponse<ProductHttpResponse> AllProducts,
    IReadOnlyList<FavoriteProductHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
