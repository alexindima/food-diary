using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Common;

internal static class RecipeImageAssetResolver {
    public static async Task<Result<IReadOnlyList<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage>?>> ResolveGalleryAsync(
        IReadOnlyList<Guid>? ids, UserId userId, IImageAssetAccessService service, CancellationToken cancellationToken) {
        if (ids is null) { return Result.Success<IReadOnlyList<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage>?>(value: null); }
        if (ids.Count > 5 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Count) {
            return Result.Failure<IReadOnlyList<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage>?>(FoodDiary.Application.Abstractions.Common.Abstractions.Results.Errors.Validation.Invalid("ImageAssetIds", "Provide up to five distinct image IDs."));
        }
        List<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage> images = [];
        foreach (Guid id in ids) {
            Result<ImageAssetResolution> resolved = await ImageAssetResolver.ResolveOptionalAsync(id, "ImageAssetIds", userId, service, cancellationToken).ConfigureAwait(false);
            if (resolved.IsFailure) { return Result.Failure<IReadOnlyList<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage>?>(resolved.Error); }
            images.Add(new FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage(resolved.Value.ImageAssetId!.Value, resolved.Value.ImageAsset!.Url, images.Count));
        }
        return Result.Success<IReadOnlyList<FoodDiary.Modules.Recipes.Domain.Entities.RecipeImage>?>(images);
    }
}
