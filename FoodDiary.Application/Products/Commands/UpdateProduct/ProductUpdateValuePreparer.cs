using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal static class ProductUpdateValuePreparer {
    public static async Task<Result<ProductUpdateValues>> PrepareAsync(
        UpdateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ProductUpdateValues, ProductId>(productIdResult);
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

        return Result.Success(new ProductUpdateValues(
            userId,
            productId,
            unitResult.Value,
            visibilityResult.Value,
            productTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageUrl,
            imageAssetResult.Value.HasResolvedImageAsset));
    }

    private static async Task<Result<UserId>> ResolveUserIdAsync(
        UpdateProductCommand command,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserId>(Errors.Authentication.InvalidToken);
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
