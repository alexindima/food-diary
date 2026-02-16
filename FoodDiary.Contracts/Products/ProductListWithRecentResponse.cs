using FoodDiary.Contracts.Common;

namespace FoodDiary.Contracts.Products;

public record ProductListWithRecentResponse(
    IReadOnlyList<ProductResponse> RecentItems,
    PagedResponse<ProductResponse> AllProducts);
