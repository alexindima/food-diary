using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
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
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FavoriteProductModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FavoriteProductModel>(accessError);
        }

        var favoriteProductId = new FavoriteProductId(command.FavoriteProductId);
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
