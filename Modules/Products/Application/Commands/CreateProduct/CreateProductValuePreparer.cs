using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.CreateProduct;

internal static class CreateProductValuePreparer {
    public static async Task<Result<CreateProductValues>> PrepareAsync(
        CreateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(command, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<CreateProductValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<MeasurementUnit> baseUnitResult = ProductCommandParsers.ParseRequiredBaseUnit(
            command.BaseUnit,
            nameof(command.BaseUnit));
        if (baseUnitResult.IsFailure) {
            return Result.Failure<CreateProductValues>(baseUnitResult.Error);
        }

        Result<Visibility> visibilityResult = ProductCommandParsers.ParseRequiredVisibility(
            command.Visibility,
            nameof(command.Visibility));
        if (visibilityResult.IsFailure) {
            return Result.Failure<CreateProductValues>(visibilityResult.Error);
        }

        Result<ProductType> productTypeResult = ProductCommandParsers.ParseRequiredProductType(
            command.ProductType,
            nameof(command.ProductType));
        if (productTypeResult.IsFailure) {
            return Result.Failure<CreateProductValues>(productTypeResult.Error);
        }

        Result<ProductImageAssetResolution> imageAssetResult = await ProductImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            command.ImageUrl,
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateProductValues>(imageAssetResult.Error);
        }

        Result<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?> gallery = await ProductImageAssetResolver.ResolveGalleryAsync(command.ImageAssetIds, userId, imageAssetAccessService, cancellationToken).ConfigureAwait(false);
        if (gallery.IsFailure) { return Result.Failure<CreateProductValues>(gallery.Error); }

        return Result.Success(new CreateProductValues(
            userId,
            baseUnitResult.Value,
            visibilityResult.Value,
            productTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageUrl) { Images = gallery.Value });
    }

    private static async Task<Result<UserId>> ResolveUserIdAsync(
        CreateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return userIdResult;
        }

        return userIdResult;
    }

}
