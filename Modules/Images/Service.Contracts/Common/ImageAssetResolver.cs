using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public static class ImageAssetResolver {
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

        Result<ImageAssetReadModel?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<ImageAssetResolution>(imageAssetResult.Error);
        }

        ImageAssetResolution resolution = imageAssetResult.Value is { } asset
            ? new ImageAssetResolution.Resolved(imageAssetIdResult.Value, asset)
            : new ImageAssetResolution.Unresolved(imageAssetIdResult.Value);
        return Result.Success(resolution);
    }
}
