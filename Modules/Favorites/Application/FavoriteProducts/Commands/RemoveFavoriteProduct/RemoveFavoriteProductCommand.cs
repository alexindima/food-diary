using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public record RemoveFavoriteProductCommand(
    Guid? UserId,
    Guid FavoriteProductId) : ICommand<Result>, IUserRequest;
