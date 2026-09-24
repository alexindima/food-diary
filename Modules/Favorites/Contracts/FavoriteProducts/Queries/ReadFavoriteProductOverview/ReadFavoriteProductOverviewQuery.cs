using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProductOverview;

// Trusted owner operation: callers authorize the user and supplied product scope.
public sealed record ReadFavoriteProductOverviewQuery(UserId UserId, IReadOnlyCollection<ProductId> ProductIds, int PreviewLimit = 0)
    : IQuery<FavoriteProductOverviewModel>;
