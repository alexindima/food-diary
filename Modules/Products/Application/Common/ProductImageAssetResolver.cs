using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Images.Common;
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
        Result<ImageAssetResolution> resolutionResult = await ImageAssetResolver.ResolveOptionalAsync(
            imageAssetId,
            propertyName,
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        return resolutionResult.IsFailure
            ? Result.Failure<ProductImageAssetResolution>(resolutionResult.Error)
            : Result.Success(new ProductImageAssetResolution(
                resolutionResult.Value.ImageAssetId,
                resolutionResult.Value.ImageAsset?.Url ?? fallbackImageUrl,
                resolutionResult.Value.ImageAsset is not null));
    }
}
