using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class RecipeModerationIntegrationTests {
    [RequiresDockerFact]
    public async Task PromotionWaitsForDurableApprovalAndKeepsCurrentPublicationDuringRetriesAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("photo-author@example.com", "hash");
        var reader = User.Create("photo-reader@example.com", "hash");
        var live = Recipe.Create(author.Id, "Approved recipe", servings: 2);
        live.AddStep(1, "Cook.");
        var photo = ImageAsset.Create(author.Id, "moderation/photo.jpg", "https://cdn.example.com/photo.jpg");
        database.AddRange(author, reader, live, photo);
        await database.SaveChangesAsync();

        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        IRecipeImagePromotionService photoPromotion = Substitute.For<IRecipeImagePromotionService>();
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi,
            photoPromotion: photoPromotion, privateAssets: [photo], clock: clock);
        RecipeModel pending = await ReplacePendingAsync(provider, author.Id, live.Id.Value, "Revised recipe", [photo.Id.Value]);
        Guid revisionId = GetRevisionId(pending);
        Assert.True(await ProcessAsync(provider));
        await photoPromotion.DidNotReceive().PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());

        const int automaticAttempts = 6;
        int calls = 0;
        photoPromotion.PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>()).Returns(async _ => {
            RecipePublicationRevision approved = await database.Set<RecipePublicationRevision>().AsNoTracking()
                .SingleAsync(revision => revision.Id == revisionId);
            Assert.Equal(RecipePublicationStatuses.Approved, approved.Status);
            Assert.True(approved.IsCurrent);
            Assert.NotNull(approved.PrimaryResult);
            Assert.NotNull(approved.LeaseToken);
            calls++;
            return calls <= automaticAttempts
                ? Result.Failure(new Error("Images.FixtureUnavailable", "Synthetic storage failure."))
                : Result.Success();
        });

        for (int attempt = 1; attempt <= automaticAttempts; attempt++) {
            Assert.True(await ProcessAsync(provider));
            Assert.Equal("Approved recipe", (await ReadRecipeAsync(provider, live.Id, reader.Id))?.Name);
            clock.AdvanceHour();
        }

        RecipeModerationItem delayed = await GetItemAsync(provider, revisionId);
        Assert.Equal(RecipePublicationStatuses.CheckDelayed, delayed.Status);
        Assert.Equal(RecipePublicationStatuses.PhotoPromotionUnavailable, delayed.Reason);
        Assert.False(await ProcessAsync(provider));

        await using (AsyncServiceScope scope = provider.CreateAsyncScope()) {
            IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();
            Assert.Equal(RecipeModerationActionOutcome.Applied, await moderation.RetryAsync(revisionId, author.Id.Value, CancellationToken.None));
        }
        Assert.True(await ProcessAsync(provider));

        RecipeModerationItem published = await GetItemAsync(provider, revisionId);
        Assert.Equal(RecipePublicationStatuses.Published, published.Status);
        Assert.Equal(delayed.PrimaryResult, published.PrimaryResult);
        Assert.Null(published.Reason);
        Recipe visible = Assert.IsType<Recipe>(await ReadRecipeAsync(provider, live.Id, reader.Id));
        Assert.Equal("Revised recipe", visible.Name);
        Assert.Equal(photo.Id, visible.ImageAssetId);
        await openAi.Received(1).CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        await photoPromotion.Received(automaticAttempts + 1).PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());
    }

    [RequiresDockerFact]
    public async Task SupersessionDuringPromotionStopsRemainingPhotosAndKeepsCurrentPublicationAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("supersession-author@example.com", "hash");
        var reader = User.Create("supersession-reader@example.com", "hash");
        var live = Recipe.Create(author.Id, "Approved recipe", servings: 2);
        live.AddStep(1, "Cook.");
        var firstPhoto = ImageAsset.Create(author.Id, "moderation/first.jpg", "https://cdn.example.com/first.jpg");
        var secondPhoto = ImageAsset.Create(author.Id, "moderation/second.jpg", "https://cdn.example.com/second.jpg");
        database.AddRange(author, reader, live, firstPhoto, secondPhoto);
        await database.SaveChangesAsync();

        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        IRecipeImagePromotionService photoPromotion = Substitute.For<IRecipeImagePromotionService>();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi,
            photoPromotion: photoPromotion, privateAssets: [firstPhoto, secondPhoto]);
        RecipeModel pending = await ReplacePendingAsync(provider, author.Id, live.Id.Value, "Superseded edit", [firstPhoto.Id.Value, secondPhoto.Id.Value]);
        Guid revisionId = GetRevisionId(pending);
        Guid replacementId = Guid.Empty;
        photoPromotion.PromoteAsync(firstPhoto.Id, author.Id, Arg.Any<CancellationToken>()).Returns(async _ => {
            RecipeModel replacement = await ReplacePendingAsync(provider, author.Id, live.Id.Value, "Latest edit");
            replacementId = GetRevisionId(replacement);
            return Result.Success();
        });

        Assert.True(await ProcessAsync(provider));
        Assert.True(await ProcessAsync(provider));

        Assert.NotEqual(Guid.Empty, replacementId);
        Assert.Equal(RecipePublicationStatuses.Superseded, (await GetItemAsync(provider, revisionId)).Status);
        Assert.Equal(RecipePublicationStatuses.Pending, (await GetItemAsync(provider, replacementId)).Status);
        Assert.Equal("Approved recipe", (await ReadRecipeAsync(provider, live.Id, reader.Id))?.Name);
        await photoPromotion.Received(1).PromoteAsync(firstPhoto.Id, author.Id, Arg.Any<CancellationToken>());
        await photoPromotion.DidNotReceive().PromoteAsync(secondPhoto.Id, author.Id, Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class ModerationClock(DateTimeOffset now) : TimeProvider {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void AdvanceHour() => _now = _now.AddHours(1);
    }
}
