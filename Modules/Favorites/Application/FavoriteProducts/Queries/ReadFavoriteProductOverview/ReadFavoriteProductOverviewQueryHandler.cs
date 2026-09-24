using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProductOverview;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProductOverview;

public sealed class ReadFavoriteProductOverviewQueryHandler(IFavoriteProductQuery queries)
    : IQueryHandler<ReadFavoriteProductOverviewQuery, FavoriteProductOverviewModel> {
    public async Task<FavoriteProductOverviewModel> Handle(ReadFavoriteProductOverviewQuery request, CancellationToken cancellationToken) {
        (IReadOnlyList<FavoriteProductReadModel> preview, int total) = await queries.GetPageReadModelsAsync(request.UserId, 1, Math.Clamp(request.PreviewLimit, 0, 50), search: null, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FavoriteProductReadModel> items = await queries.GetByProductIdsReadModelsAsync(request.UserId, request.ProductIds, cancellationToken).ConfigureAwait(false);
        return new FavoriteProductOverviewModel(items.Select(item => item.ToModel()).ToArray(), preview.Select(item => item.ToModel()).ToArray(), total);
    }
}
