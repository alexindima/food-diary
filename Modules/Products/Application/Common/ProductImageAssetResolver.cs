using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Common;

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
