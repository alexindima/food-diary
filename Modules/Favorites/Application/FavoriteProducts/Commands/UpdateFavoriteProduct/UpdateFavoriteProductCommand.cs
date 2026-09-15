using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;

public record UpdateFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId,
    string? Name,
    double PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
