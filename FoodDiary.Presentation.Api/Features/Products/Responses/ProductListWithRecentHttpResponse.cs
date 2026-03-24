using FoodDiary.Application.Common.Models;

namespace FoodDiary.Presentation.Api.Features.Products.Responses;

public sealed record ProductListWithRecentHttpResponse(
    IReadOnlyList<ProductHttpResponse> RecentItems,
    PagedResponse<ProductHttpResponse> AllProducts);
