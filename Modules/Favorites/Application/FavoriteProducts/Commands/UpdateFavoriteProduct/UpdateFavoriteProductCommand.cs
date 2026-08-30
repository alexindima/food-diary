using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;

namespace FoodDiary.Application.Favorites.FavoriteProducts.Commands.UpdateFavoriteProduct;

public record UpdateFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId,
    string? Name,
    double PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
