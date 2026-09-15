using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Mappings;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProducts;

public sealed class ReadFavoriteProductsQueryHandler(IFavoriteProductReadModelRepository favoriteProductReadModelRepository) : IQueryHandler<ReadFavoriteProductsQuery, IReadOnlyList<FavoriteProductModel>> {
    public async Task<IReadOnlyList<FavoriteProductModel>> Handle(ReadFavoriteProductsQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        IReadOnlyList<FavoriteProductReadModel> favorites = await favoriteProductReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

}
