using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public record RemoveFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId) : ICommand<Result>, IUserRequest;
