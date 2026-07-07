using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;

public sealed class UpdateFavoriteProductCommandHandler(
    IFavoriteProductWriteRepository favoriteProductRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateFavoriteProductCommand, Result<FavoriteProductModel>> {
    public async Task<Result<FavoriteProductModel>> Handle(
        UpdateFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FavoriteProductModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<FavoriteProductId> favoriteProductIdResult = RequiredIdParser.Parse(
            command.FavoriteProductId,
            nameof(command.FavoriteProductId),
            "Favorite product id must not be empty.",
            value => new FavoriteProductId(value));
        if (favoriteProductIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<FavoriteProductModel, FavoriteProductId>(favoriteProductIdResult);
        }

        FavoriteProductId favoriteProductId = favoriteProductIdResult.Value;
        FavoriteProduct? favorite = await favoriteProductRepository.GetByIdAsync(
            favoriteProductId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure<FavoriteProductModel>(Errors.FavoriteProduct.NotFound(command.FavoriteProductId));
        }

        favorite.UpdateName(command.Name);
        favorite.UpdatePreferredPortionAmount(command.PreferredPortionAmount);

        await favoriteProductRepository.UpdateAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorite.ToModel());
    }
}
