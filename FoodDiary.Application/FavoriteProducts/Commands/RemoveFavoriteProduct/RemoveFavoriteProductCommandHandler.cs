using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public class RemoveFavoriteProductCommandHandler(
    IFavoriteProductRepository favoriteProductRepository,
    IUserRepository userRepository)
    : ICommandHandler<RemoveFavoriteProductCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var favoriteProductId = new FavoriteProductId(command.FavoriteProductId);
        var favorite = await favoriteProductRepository.GetByIdAsync(
            favoriteProductId, userId, asTracking: true, cancellationToken: cancellationToken);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteProduct.NotFound(command.FavoriteProductId));
        }

        await favoriteProductRepository.DeleteAsync(favorite, cancellationToken);
        return Result.Success();
    }
}
