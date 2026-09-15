using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteProducts.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Products.Presentation.Responses;

public sealed record ProductOverviewHttpResponse(
    IReadOnlyList<ProductHttpResponse> RecentItems,
    PagedHttpResponse<ProductHttpResponse> AllProducts,
    IReadOnlyList<FavoriteProductHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
