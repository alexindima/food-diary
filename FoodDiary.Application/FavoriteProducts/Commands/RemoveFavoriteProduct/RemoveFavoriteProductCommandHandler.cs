using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public class RemoveFavoriteProductCommandHandler(
    IFavoriteProductWriteRepository favoriteProductRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RemoveFavoriteProductCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var favoriteProductId = new FavoriteProductId(command.FavoriteProductId);
        FavoriteProduct? favorite = await favoriteProductRepository.GetByIdAsync(
            favoriteProductId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteProduct.NotFound(command.FavoriteProductId));
        }

        await favoriteProductRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
