using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public record AddFavoriteProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Name,
    double? PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
