using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

public sealed class RemoveFavoriteProductCommandHandler(
    IFavoriteProductWriteRepository favoriteProductRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RemoveFavoriteProductCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<FavoriteProductId> favoriteProductIdResult = RequiredIdParser.Parse(
            command.FavoriteProductId,
            nameof(command.FavoriteProductId),
            "Favorite product id must not be empty.",
            value => new FavoriteProductId(value));
        if (favoriteProductIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(favoriteProductIdResult);
        }

        FavoriteProductId favoriteProductId = favoriteProductIdResult.Value;
        FavoriteProduct? favorite = await favoriteProductRepository.GetByIdAsync(
            favoriteProductId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteProduct.NotFound(command.FavoriteProductId));
        }

        await favoriteProductRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
