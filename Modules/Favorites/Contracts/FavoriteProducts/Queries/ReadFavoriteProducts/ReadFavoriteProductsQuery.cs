using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadFavoriteProductsQuery(UserId UserId) : IQuery<IReadOnlyList<FavoriteProductModel>>;
