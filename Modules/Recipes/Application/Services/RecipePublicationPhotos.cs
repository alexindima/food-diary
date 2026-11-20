using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Services;

internal static class RecipePublicationPhotos {
    public static async Task<Result> EnsurePrivateUploadsAsync(
        RecipeModerationContent content,
        UserId ownerId,
        IImageAssetAccessService imageAccess,
        IReadOnlySet<Guid>? retained,
        CancellationToken cancellationToken) {
        foreach (Guid id in AssetIds(content)) {
            if (retained?.Contains(id) == true) {
                continue;
            }

            Result<ImageAssetReadModel?> image = await imageAccess.ResolveOptionalAsync((ImageAssetId)id, ownerId, cancellationToken);
            if (image.IsFailure) {
                return Result.Failure(image.Error);
            }
            if (image.Value?.IsPrivate != true) {
                return Result.Failure(RecipeErrors.PublicationImageRequiresPrivateUpload);
            }
        }

        return Result.Success();
    }

    public static IReadOnlySet<Guid> GetRetainedAssets(Recipe recipe, RecipePublicationDraft? previous) {
        var retained = new HashSet<Guid>();
        if (recipe.Visibility == Visibility.Public) {
            retained.UnionWith(AssetIds(RecipePublicationDraft.BuildContent(recipe)));
        }
        if (previous is not null) {
            UpdateRecipeCommand change = previous.Change;
            AddIds(retained, change.ImageAssetId, change.ImageAssetIds);
            foreach (RecipeStepInput step in change.Steps ?? []) {
                AddIds(retained, step.ImageAssetId, step.ImageAssetIds);
            }
        }

        return retained;
    }

    private static IEnumerable<Guid> AssetIds(RecipeModerationContent content) =>
        content.Images.Select(image => image.AssetId).OfType<Guid>().Distinct();

    private static void AddIds(HashSet<Guid> ids, Guid? coverId, IReadOnlyList<Guid>? galleryIds) {
        if (coverId is { } id) {
            ids.Add(id);
        }
        if (galleryIds is not null) {
            ids.UnionWith(galleryIds);
        }
    }
}
