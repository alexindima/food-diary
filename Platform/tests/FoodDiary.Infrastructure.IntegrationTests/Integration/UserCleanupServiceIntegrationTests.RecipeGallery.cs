using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupDeletedUsersAsync_WithRecipeGalleries_ReassignsEveryReferencedAssetAtomically(bool failAfterReassignment) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        (User deleted, User survivor) = await SeedReassignScenarioAsync(context);
        RecipeGalleryPurgeScenario scenario = await AddRecipeGalleriesAsync(context, deleted);
        context.ChangeTracker.Clear();
        var outbox = new RecordingImageObjectDeletionOutbox();
        await using ServiceProvider provider = CreateServiceProvider(context, outbox, out UserCleanupService service,
            failAfterReassignment ? new FailingParticipant(context) : null);

        UserCleanupBatch batch = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, survivor.Id.Value);

        await using FoodDiaryDbContext verification = CreateVerificationContext(context);
        Recipe galleryRecipe = await verification.Recipes.AsNoTracking().Include(recipe => recipe.Images)
            .Include(recipe => recipe.Steps).ThenInclude(step => step.Images).SingleAsync(recipe => recipe.Id == scenario.RecipeId);
        Recipe legacyRecipe = await verification.Recipes.AsNoTracking().Include(recipe => recipe.Steps)
            .SingleAsync(recipe => recipe.Id == scenario.LegacyRecipeId);
        Recipe foreignRecipe = await verification.Recipes.AsNoTracking().Include(recipe => recipe.Images)
            .SingleAsync(recipe => recipe.Id == scenario.ForeignRecipeId);
        RecipeStep galleryStep = Assert.Single(galleryRecipe.Steps);
        RecipeStep legacyStep = Assert.Single(legacyRecipe.Steps);
        List<ImageAsset> retainedAssets = await verification.ImageAssets.AsNoTracking().ToListAsync();
        ImageAsset foreignAsset = Assert.Single(retainedAssets, asset => asset.Id == scenario.ForeignAsset.Id);
        bool deletedUserExists = await verification.Users.AnyAsync(user => user.Id == deleted.Id);
        UserId expectedOwner = failAfterReassignment ? deleted.Id : survivor.Id;

        Assert.Multiple(
            () => Assert.Equal(failAfterReassignment ? 0 : 1, batch.RemovedCount),
            () => Assert.Equal(failAfterReassignment, deletedUserExists),
            () => Assert.Equal(expectedOwner, galleryRecipe.UserId),
            () => Assert.Equal(expectedOwner, legacyRecipe.UserId),
            () => Assert.Equal(scenario.ForeignAsset.UserId, foreignRecipe.UserId),
            () => Assert.Equal(scenario.ForeignAsset.UserId, foreignAsset.UserId),
            () => Assert.Equal(scenario.ForeignAsset.ObjectKey, foreignAsset.ObjectKey),
            () => Assert.Equal(scenario.ForeignAsset.Url, foreignAsset.Url),
            () => Assert.Equal(scenario.LegacyRecipeAsset.Id, legacyRecipe.ImageAssetId),
            () => Assert.Equal(scenario.LegacyStepAsset.Id, legacyStep.ImageAssetId),
            () => Assert.Empty(legacyRecipe.Images),
            () => Assert.Empty(legacyStep.Images),
            () => Assert.Equal(scenario.RecipeImages.Select(image => image.ImageAssetId),
                galleryRecipe.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)),
            () => Assert.Equal(scenario.RecipeImages.Select(image => image.ImageUrl),
                galleryRecipe.Images.OrderBy(image => image.Position).Select(image => image.ImageUrl), StringComparer.Ordinal),
            () => Assert.Equal(scenario.StepImages.Select(image => image.ImageAssetId),
                galleryStep.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)),
            () => Assert.Equal(scenario.StepImages.Select(image => image.ImageUrl),
                galleryStep.Images.OrderBy(image => image.Position).Select(image => image.ImageUrl), StringComparer.Ordinal),
            () => Assert.Equal(scenario.ForeignAsset.Id, Assert.Single(foreignRecipe.Images).ImageAssetId),
            () => Assert.False(context.ChangeTracker.HasChanges()));

        foreach (ImageAsset original in scenario.OwnedAssets) {
            ImageAsset retained = Assert.Single(retainedAssets, asset => asset.Id == original.Id);
            Assert.Multiple(
                () => Assert.Equal(expectedOwner, retained.UserId),
                () => Assert.Equal(original.ObjectKey, retained.ObjectKey),
                () => Assert.Equal(original.Url, retained.Url));
        }
        if (!failAfterReassignment) {
            Assert.Equal(["users/deleted/meal.webp", "users/deleted/profile.webp"],
                outbox.ObjectKeys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        }
    }

    private static async Task<RecipeGalleryPurgeScenario> AddRecipeGalleriesAsync(FoodDiaryDbContext context, User deleted) {
        Recipe recipe = await context.Recipes.Include(item => item.Steps).SingleAsync();
        RecipeStep step = Assert.Single(recipe.Steps);
        ImageAsset recipeCover = await context.ImageAssets.SingleAsync(asset => asset.Id == recipe.ImageAssetId);
        ImageAsset stepCover = await context.ImageAssets.SingleAsync(asset => asset.Id == step.ImageAssetId);
        var recipeExtra = ImageAsset.Create(deleted.Id, "users/deleted/recipe-extra.webp", "https://cdn.example.com/recipe-extra.webp");
        var stepExtra = ImageAsset.Create(deleted.Id, "users/deleted/step-extra.webp", "https://cdn.example.com/step-extra.webp");
        var sharedExtra = ImageAsset.Create(deleted.Id, "users/deleted/shared-extra.webp", "https://cdn.example.com/shared-extra.webp");
        var legacyRecipeAsset = ImageAsset.Create(deleted.Id, "users/deleted/legacy-recipe.webp", "https://cdn.example.com/legacy-recipe.webp");
        var legacyStepAsset = ImageAsset.Create(deleted.Id, "users/deleted/legacy-step.webp", "https://cdn.example.com/legacy-step.webp");
        var foreignOwner = User.Create("foreign-gallery-owner@example.com", "hash");
        var foreignAsset = ImageAsset.Create(foreignOwner.Id, "users/foreign/foreign-gallery.webp", "https://cdn.example.com/foreign-gallery.webp");
        RecipeImage[] recipeImages = [new(recipeCover.Id, recipeCover.Url, 0), new(recipeExtra.Id, recipeExtra.Url, 1), new(sharedExtra.Id, sharedExtra.Url, 2), new(foreignAsset.Id, foreignAsset.Url, 3)];
        RecipeImage[] stepImages = [new(stepCover.Id, stepCover.Url, 0), new(stepExtra.Id, stepExtra.Url, 1), new(sharedExtra.Id, sharedExtra.Url, 2), new(foreignAsset.Id, foreignAsset.Url, 3)];
        recipe.ReplaceImages(recipeImages);
        step.ReplaceImages(stepImages);
        var legacyRecipe = Recipe.Create(deleted.Id, "Legacy recipe", 1, imageAssetId: legacyRecipeAsset.Id);
        legacyRecipe.AddStep(1, "Legacy step", imageAssetId: legacyStepAsset.Id);
        var foreignRecipe = Recipe.Create(foreignOwner.Id, "Foreign gallery", 1);
        foreignRecipe.ReplaceImages([new RecipeImage(foreignAsset.Id, foreignAsset.Url, 0)]);
        context.Users.Add(foreignOwner);
        context.ImageAssets.AddRange(recipeExtra, stepExtra, sharedExtra, legacyRecipeAsset, legacyStepAsset, foreignAsset);
        context.Recipes.AddRange(legacyRecipe, foreignRecipe);
        await context.SaveChangesAsync();
        ImageAsset[] ownedAssets = await context.ImageAssets.AsNoTracking()
            .Where(asset => asset.UserId == deleted.Id && asset.ObjectKey != "users/deleted/meal.webp" && asset.ObjectKey != "users/deleted/profile.webp")
            .ToArrayAsync();
        return new RecipeGalleryPurgeScenario(recipe.Id, legacyRecipe.Id, foreignRecipe.Id, recipeImages, stepImages,
            legacyRecipeAsset, legacyStepAsset, foreignAsset, ownedAssets);
    }

    [ExcludeFromCodeCoverage]
    private sealed record RecipeGalleryPurgeScenario(RecipeId RecipeId, RecipeId LegacyRecipeId, RecipeId ForeignRecipeId,
        IReadOnlyList<RecipeImage> RecipeImages, IReadOnlyList<RecipeImage> StepImages, ImageAsset LegacyRecipeAsset,
        ImageAsset LegacyStepAsset, ImageAsset ForeignAsset, IReadOnlyList<ImageAsset> OwnedAssets);
}
