using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Products.Models;

public sealed record ProductOverviewModel(
    IReadOnlyList<ProductModel> RecentItems,
    PagedResponse<ProductModel> AllProducts,
    IReadOnlyList<FavoriteProductModel> FavoriteItems,
    int FavoriteTotalCount);
