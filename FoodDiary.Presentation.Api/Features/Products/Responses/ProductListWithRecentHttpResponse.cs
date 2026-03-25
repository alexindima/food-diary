using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Products.Responses;

public sealed record ProductListWithRecentHttpResponse(
    IReadOnlyList<ProductHttpResponse> RecentItems,
    PagedHttpResponse<ProductHttpResponse> AllProducts);
