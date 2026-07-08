using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Common;

internal static class ImageAssetResolver {
    public static async Task<Result<ImageAssetResolution>> ResolveOptionalAsync(
        Guid? imageAssetId,
        string propertyName,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(imageAssetId, propertyName);
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ImageAssetResolution>(imageAssetIdResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        return imageAssetResult.IsFailure
            ? Result.Failure<ImageAssetResolution>(imageAssetResult.Error)
            : Result.Success(new ImageAssetResolution(imageAssetIdResult.Value, imageAssetResult.Value));
    }
}
