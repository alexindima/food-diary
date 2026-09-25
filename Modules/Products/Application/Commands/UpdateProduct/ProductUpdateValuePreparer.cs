using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.UpdateProduct;

internal static class ProductUpdateValuePreparer {
    public static async Task<Result<ProductUpdateValues>> PrepareAsync(
        UpdateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = ProductRequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return ProductRequiredIdParser.ToFailure<ProductUpdateValues, ProductId>(productIdResult);
        }

        Result<UserId> userIdResult = await ResolveUserIdAsync(command, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ProductUpdateValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;
        Result<MeasurementUnit?> unitResult = ProductCommandParsers.ParseOptionalBaseUnit(
            command.BaseUnit,
            nameof(command.BaseUnit));
        if (unitResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(unitResult.Error);
        }

        Result<Visibility?> visibilityResult = ProductCommandParsers.ParseOptionalVisibility(
            command.Visibility,
            nameof(command.Visibility));
        if (visibilityResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(visibilityResult.Error);
        }

        Result<ProductType?> productTypeResult = ProductCommandParsers.ParseOptionalProductType(
            command.ProductType,
            nameof(command.ProductType));
        if (productTypeResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(productTypeResult.Error);
        }

        Result<ProductImageAssetResolution> imageAssetResult = await ProductImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            command.ImageUrl,
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(imageAssetResult.Error);
        }

        Result<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?> gallery = await ProductImageAssetResolver.ResolveGalleryAsync(command.ImageAssetIds, userId, imageAssetAccessService, cancellationToken).ConfigureAwait(false);
        if (gallery.IsFailure) { return Result.Failure<ProductUpdateValues>(gallery.Error); }

        return Result.Success(new ProductUpdateValues(
            userId,
            productId,
            unitResult.Value,
            visibilityResult.Value,
            productTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageUrl,
            imageAssetResult.Value.HasResolvedImageAsset) { Images = gallery.Value });
    }

    private static async Task<Result<UserId>> ResolveUserIdAsync(
        UpdateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserId>(AuthenticationErrors.InvalidToken);
        }

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
