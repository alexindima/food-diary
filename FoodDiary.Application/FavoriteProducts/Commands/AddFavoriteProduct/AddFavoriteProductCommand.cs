using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public record AddFavoriteProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Name,
    double? PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
