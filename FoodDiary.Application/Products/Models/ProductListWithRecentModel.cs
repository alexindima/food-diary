using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Products.Models;

public sealed record ProductListWithRecentModel(
    IReadOnlyList<ProductModel> RecentItems,
    PagedResponse<ProductModel> AllProducts);
