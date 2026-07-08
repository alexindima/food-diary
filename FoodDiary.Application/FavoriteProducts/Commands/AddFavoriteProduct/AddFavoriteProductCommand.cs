using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public record AddFavoriteProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Name,
    double? PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
