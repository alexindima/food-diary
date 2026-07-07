using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Common;

internal static class ProductImageAssetResolver {
    public static async Task<Result<ProductImageAssetResolution>> ResolveOptionalAsync(
        Guid? imageAssetId,
        string propertyName,
        string? fallbackImageUrl,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(imageAssetId, propertyName);
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ProductImageAssetResolution>(imageAssetIdResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        return imageAssetResult.IsFailure
            ? Result.Failure<ProductImageAssetResolution>(imageAssetResult.Error)
            : Result.Success(new ProductImageAssetResolution(
                imageAssetIdResult.Value,
                imageAssetResult.Value?.Url ?? fallbackImageUrl,
                imageAssetResult.Value is not null));
    }
}
