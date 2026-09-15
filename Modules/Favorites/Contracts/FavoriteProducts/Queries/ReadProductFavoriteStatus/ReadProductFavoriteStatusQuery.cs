using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadProductFavoriteStatusQuery(ProductId ProductId, UserId UserId) : IQuery<bool>;
