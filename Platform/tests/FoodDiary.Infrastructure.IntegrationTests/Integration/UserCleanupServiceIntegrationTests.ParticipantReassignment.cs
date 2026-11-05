using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContentParticipants_ReassignOwnedRowsAndImagesAtomically(bool rollBack) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        (User source, User target) = await SeedReassignScenarioAsync(context);
        RecipeGalleryPurgeScenario gallery = await AddRecipeGalleriesAsync(context, source);
        Product product = await context.Products.SingleAsync();
        ImageAsset cover = await context.ImageAssets.SingleAsync(asset => asset.Id == product.ImageAssetId);
        var extra = ImageAsset.Create(source.Id, "users/deleted/product-extra.webp", "https://cdn.example.com/product-extra.webp");
        product.ReplaceImages([
            new ProductImage(cover.Id, cover.Url, 0),
            new ProductImage(extra.Id, extra.Url, 1),
            new ProductImage(gallery.ForeignAsset.Id, gallery.ForeignAsset.Url, 2),
        ]);
        context.ImageAssets.Add(extra);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out _);
        IUserDataPurgeParticipant[] participants = [.. provider.GetServices<IUserDataPurgeParticipant>()
            .Where(participant => participant.GetType().Assembly == typeof(ProductsDbContext).Assembly ||
                participant.GetType().Assembly == typeof(RecipesDbContext).Assembly).OrderBy(participant => participant.Order)];
        Assert.Equal(2, participants.Length);

        Result outcome = await provider.GetRequiredService<IModuleTransactionCoordinator>().ExecuteAsync(async (_, token) => {
            foreach (IUserDataPurgeParticipant participant in participants) {
                await participant.PurgeAsync(source.Id, target.Id, token);
            }
            return rollBack ? Result.Failure(new Error("Test.Rollback", "Verify atomic content transfer.")) : Result.Success();
        }, CancellationToken.None);

        UserId expectedOwner = rollBack ? source.Id : target.Id;
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.Equal(rollBack, outcome.IsFailure);
        Assert.Equal(expectedOwner, (await observer.Products.SingleAsync(item => item.Id == product.Id)).UserId);
        Assert.Equal(expectedOwner, (await observer.Recipes.SingleAsync(item => item.Id == gallery.RecipeId)).UserId);
        Assert.Equal(expectedOwner, (await observer.Recipes.SingleAsync(item => item.Id == gallery.LegacyRecipeId)).UserId);
        foreach (ImageAsset asset in gallery.OwnedAssets.Append(extra)) {
            Assert.Equal(expectedOwner, (await observer.ImageAssets.SingleAsync(item => item.Id == asset.Id)).UserId);
        }
        Assert.Equal(gallery.ForeignAsset.UserId,
            (await observer.ImageAssets.SingleAsync(item => item.Id == gallery.ForeignAsset.Id)).UserId);
        Assert.Equal(2, await observer.ImageAssets.CountAsync(asset => asset.UserId == source.Id &&
            (asset.ObjectKey == "users/deleted/meal.webp" || asset.ObjectKey == "users/deleted/profile.webp")));
        Assert.True(await observer.Users.AnyAsync(user => user.Id == source.Id));
        Assert.True(await observer.Users.AnyAsync(user => user.Id == target.Id));
    }
}
