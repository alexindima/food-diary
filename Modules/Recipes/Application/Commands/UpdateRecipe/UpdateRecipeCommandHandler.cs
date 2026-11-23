using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;

using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IRecipeNutritionWriter recipeNutritionWriter,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    IImageAssetAccessService imageAssetAccessService,
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService,
    IRecipeMutationTransactionRunner transactionRunner,
    IRecipePublicationQueue? publicationQueue = null)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeModel>> {
    public Task<Result<RecipeModel>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync(
            token => HandleCoreAsync(command, token),
            cancellationToken);

    private async Task<Result<RecipeModel>> HandleCoreAsync(UpdateRecipeCommand command, CancellationToken cancellationToken) {
        Result<UpdateRecipeValues> valuesResult = await UpdateRecipeValuePreparer.PrepareAsync(
            command,
            recipeReadRepository,
            currentUserAccessService,
            imageAssetAccessService,
            productLookupService,
            recipeLookupService,
            cancellationToken);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        UpdateRecipeValues values = valuesResult.Value;
        if (command.ApprovedPublication is { } claim) {
            if (publicationQueue is null || !await publicationQueue.CanApplyAsync(values.RecipeId, claim, cancellationToken)) {
                return PublicationClaimExpired();
            }
            if (RecipePublicationDraft.Capture(values.Recipe).HasSamePublicContent(command)) {
                values.Recipe.ChangeVisibility(Visibility.Public);
                await recipeWriteRepository.UpdateAsync(values.Recipe, cancellationToken);
                if (!await publicationQueue.CompleteAsync(values.RecipeId, claim, cancellationToken)) {
                    return PublicationClaimExpired();
                }
                return Result.Success(values.Recipe.ToModel(values.UsageCount, isOwnedByCurrentUser: true));
            }
            if (values.UsageCount > 0) {
                return Result.Failure<RecipeModel>(
                    Errors.Validation.Invalid("RecipeId", "Recipe is already used and cannot be modified"));
            }
        }

        Result<IReadOnlyList<RecipeImage>?> gallery = await RecipeImageAssetResolver.ResolveGalleryAsync(command.ImageAssetIds, values.UserId, imageAssetAccessService, cancellationToken);
        if (gallery.IsFailure) { return Result.Failure<RecipeModel>(gallery.Error); }

        if (command.ApprovedPublication is null && publicationQueue is not null) {
            RecipePublicationState? current = await publicationQueue.GetCurrentAsync(values.RecipeId, cancellationToken);
            if (values.Visibility == Visibility.Private) {
                if (publicationQueue.Enabled || current is not null) {
                    await publicationQueue.CancelAsync(values.RecipeId, cancellationToken);
                }
            } else if ((publicationQueue.Enabled || current is not null) && (values.Visibility == Visibility.Public ||
                values.Recipe.Visibility == Visibility.Public || current is not null)) {
                return await StagePublicationAsync(command, values, gallery.Value, current, publicationQueue, cancellationToken);
            }
        }

        var previousGallery = values.Recipe.Images.Select(image => image.ImageAssetId).ToList();
        Result applied = await ApplyChangesAsync(values.Recipe, command, values, gallery.Value, cancellationToken);
        if (applied.IsFailure) {
            return Result.Failure<RecipeModel>(applied.Error);
        }

        Result<Recipe> updatedResult = await SaveAndReloadAsync(values.Recipe, values.UserId, cancellationToken);
        if (updatedResult.IsFailure) {
            return Result.Failure<RecipeModel>(updatedResult.Error);
        }

        await RecipeUpdateAssetCleanup.DeleteUnusedAsync(
            command,
            values,
            imageAssetCleanupService,
            cancellationToken);
        foreach (ImageAssetId assetId in previousGallery.Where(id => !values.Recipe.Images.Any(image => image.ImageAssetId == id))) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId, cancellationToken);
        }
        Recipe updated = updatedResult.Value;
        if (command.ApprovedPublication is { } approvedClaim &&
            !await publicationQueue!.CompleteAsync(values.RecipeId, approvedClaim, cancellationToken)) {
            return PublicationClaimExpired();
        }
        return Result.Success(updated.ToModel(usageCount: 0, isOwnedByCurrentUser: true));
    }

    private async Task<Result<RecipeModel>> StagePublicationAsync(
        UpdateRecipeCommand command,
        UpdateRecipeValues values,
        IReadOnlyList<RecipeImage>? gallery,
        RecipePublicationState? current,
        IRecipePublicationQueue queue,
        CancellationToken cancellationToken) {
        var existing = RecipePublicationDraft.Read(current?.ChangeJson);
        Recipe proposed = RecipePublicationDraft.ForEdit(values.Recipe, existing);
        Result applied = await ApplyChangesAsync(proposed, command, values with { Visibility = Visibility.Public }, gallery, cancellationToken);
        if (applied.IsFailure) {
            return Result.Failure<RecipeModel>(applied.Error);
        }

        Recipe recipe = values.Recipe;
        var draft = RecipePublicationDraft.Capture(proposed);
        bool unchanged = (existing is not null || recipe.Visibility == Visibility.Public) &&
            draft.HasSamePublicContent(existing ?? RecipePublicationDraft.Capture(recipe));

        string? previousComment = recipe.Comment;
        recipe.UpdateIdentityChanges(new RecipeIdentityChanges(
            Name: null,
            Description: FieldChanges.Unchanged<string>(),
            Comment: FieldChanges.FromOptionalText(command.Comment, command.ClearComment),
            Category: FieldChanges.Unchanged<string>()));
        if (!string.Equals(recipe.Comment, previousComment, StringComparison.Ordinal)) {
            await recipeWriteRepository.UpdateAsync(recipe, cancellationToken);
        }

        RecipePublicationState? state = current;
        if (!unchanged) {
            RecipeModerationContent content = RecipePublicationDraft.BuildContent(proposed);
            Result photos = await RecipePublicationPhotos.EnsurePrivateUploadsAsync(
                content, recipe.UserId, imageAssetAccessService, RecipePublicationPhotos.GetRetainedAssets(recipe, existing), cancellationToken);
            if (photos.IsFailure) {
                return Result.Failure<RecipeModel>(photos.Error);
            }

            string changeJson = draft.Serialize();
            state = await queue.StageAsync(recipe.Id, recipe.UserId, changeJson, content, cancellationToken);
            state = state with { ChangeJson = changeJson };
        }
        return Result.Success(RecipePublicationDraft.WithPublication(
            recipe.ToModel(usageCount: 0, isOwnedByCurrentUser: true), state,
            includePreview: true));
    }

    private async Task<Result> ApplyChangesAsync(
        Recipe recipe,
        UpdateRecipeCommand command,
        UpdateRecipeValues values,
        IReadOnlyList<RecipeImage>? gallery,
        CancellationToken cancellationToken) {
        bool applyingCheckedSnapshot = command.ApprovedPublication is not null;
        if (applyingCheckedSnapshot && (gallery is not null || recipe.Images.Count > 0)) {
            recipe.ReplaceImages(gallery ?? []);
        }
        RecipeUpdateApplier.Apply(recipe, command, values);
        if (!applyingCheckedSnapshot && gallery is not null) {
            recipe.ReplaceImages(gallery);
        }

        Result stepsResult = await RecipeStepAppender.ReplaceAsync(
            recipe, values.Steps, values.UserId, imageAssetAccessService, cancellationToken);
        return stepsResult.IsFailure
            ? stepsResult
            : RecipeNutritionApplier.Apply(recipe, command.CalculateNutritionAutomatically, command.ManualCalories,
                command.ManualProteins, command.ManualFats, command.ManualCarbs, command.ManualFiber, command.ManualAlcohol);
    }

    private static Result<RecipeModel> PublicationClaimExpired() =>
        Result.Failure<RecipeModel>(RecipeErrors.InvalidData("This recipe publication is no longer current."));

    private async Task<Result<Recipe>> SaveAndReloadAsync(
        Recipe recipe,
        UserId userId,
        CancellationToken cancellationToken) {
        await recipeWriteRepository.UpdateAsync(recipe, cancellationToken);

        Recipe? updated = await recipeReadRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (updated is null) {
            return Result.Failure<Recipe>(RecipeErrors.InvalidData("Failed to load updated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(updated, recipeNutritionWriter, cancellationToken);
        return Result.Success(updated);
    }

}
