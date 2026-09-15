using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;

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
        FavoriteProduct? favorite = await favoriteProductRepository.GetOwnedByIdAsync(
            favoriteProductId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(FavoriteProductErrors.NotFound(command.FavoriteProductId));
        }

        await favoriteProductRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
