using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadProductFavoriteStatus;

public sealed class ReadProductFavoriteStatusQueryHandler(IFavoriteProductReadModelRepository favoriteProductReadModelRepository) : IQueryHandler<ReadProductFavoriteStatusQuery, bool> {
    public Task<bool> Handle(ReadProductFavoriteStatusQuery request, CancellationToken cancellationToken) {
        ProductId productId = request.ProductId;
        UserId userId = request.UserId;
        return favoriteProductReadModelRepository.ExistsByProductIdAsync(productId, userId, cancellationToken);
    }

}
