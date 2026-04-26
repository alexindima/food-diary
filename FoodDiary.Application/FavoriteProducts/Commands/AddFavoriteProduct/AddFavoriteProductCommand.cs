using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public record AddFavoriteProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Name) : ICommand<Result<FavoriteProductModel>>, IUserRequest;
