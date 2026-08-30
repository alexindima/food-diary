using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;

namespace FoodDiary.Application.Favorites.FavoriteProducts.Commands.AddFavoriteProduct;

public record AddFavoriteProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Name,
    double? PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
