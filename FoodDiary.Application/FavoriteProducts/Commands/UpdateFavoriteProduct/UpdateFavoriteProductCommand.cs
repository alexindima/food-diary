using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;

public record UpdateFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId,
    string? Name,
    double PreferredPortionAmount) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
