using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProductOverview;

public sealed record FavoriteProductOverviewModel(IReadOnlyList<FavoriteProductModel> Items,
    IReadOnlyList<FavoriteProductModel> Preview, int Total);
