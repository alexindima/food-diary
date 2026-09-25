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
    public static async Task<Result<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?>> ResolveGalleryAsync(
        IReadOnlyList<Guid>? ids, UserId userId, IImageAssetAccessService service, CancellationToken cancellationToken) {
        if (ids is null) { return Result.Success<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?>(value: null); }
        if (ids.Count > 5 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Count) {
            return Result.Failure<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?>(FoodDiary.Application.Abstractions.Common.Abstractions.Results.Errors.Validation.Invalid("ImageAssetIds", "Provide up to five distinct image IDs."));
        }
        List<FoodDiary.Modules.Products.Domain.Entities.ProductImage> images = [];
        foreach (Guid id in ids) {
            Result<ProductImageAssetResolution> resolved = await ResolveOptionalAsync(id, "ImageAssetIds", fallbackImageUrl: null, userId, service, cancellationToken).ConfigureAwait(false);
            if (resolved.IsFailure) { return Result.Failure<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?>(resolved.Error); }
            images.Add(new FoodDiary.Modules.Products.Domain.Entities.ProductImage(resolved.Value.ImageAssetId!.Value, resolved.Value.ImageUrl!, images.Count));
        }
        return Result.Success<IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>?>(images);
    }
}
