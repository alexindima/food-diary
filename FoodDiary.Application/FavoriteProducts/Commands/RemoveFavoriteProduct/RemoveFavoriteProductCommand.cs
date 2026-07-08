using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public record RemoveFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId) : ICommand<Result>, IUserRequest;
