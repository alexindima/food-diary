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
        RecipePublicationRevision staged = await database.Set<RecipePublicationRevision>().AsNoTracking()
            .Include(revision => revision.Images).SingleAsync(revision => revision.Id == revisionId);
        RecipePublicationImageReference reference = Assert.Single(staged.Images);
        Assert.Multiple(
            () => Assert.Equal(photo.Id, reference.AssetId),
            () => Assert.Equal(revisionId, reference.RevisionId));
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
            clock.Advance(TimeSpan.FromHours(1));
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

    [RequiresDockerFact]
    public async Task InterruptedFinalPromotionAttemptIsNotRepeatedAfterTheLeaseExpiresAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        var live = Recipe.Create(author.Id, "Approved recipe", servings: 2);
        var photo = ImageAsset.Create(author.Id, "moderation/interrupted.jpg", "https://cdn.example.com/interrupted.jpg");
        database.AddRange(live, photo);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        IRecipeImagePromotionService photoPromotion = Substitute.For<IRecipeImagePromotionService>();
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi,
            photoPromotion: photoPromotion, privateAssets: [photo], clock: clock);
        Guid revisionId = GetRevisionId(await ReplacePendingAsync(provider, author.Id, live.Id.Value, "Revised recipe", [photo.Id.Value]));
        Assert.True(await ProcessAsync(provider));

        // A worker stopped after committing its final claim and before recording the promotion outcome.
        await database.Set<RecipePublicationRevision>().Where(revision => revision.Id == revisionId)
            .ExecuteUpdateAsync(update => update.SetProperty(revision => revision.PromotionAttempts, 6)
                .SetProperty(revision => revision.LeaseToken, (Guid?)Guid.NewGuid())
                .SetProperty(revision => revision.LeaseUntil, (DateTimeOffset?)clock.GetUtcNow().AddMinutes(5)));
        Assert.False(await ProcessAsync(provider));

        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(await ProcessAsync(provider));

        RecipePublicationRevision delayed = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.CheckDelayed, delayed.Status),
            () => Assert.Equal(RecipePublicationStatuses.PhotoPromotionUnavailable, delayed.Reason),
            () => Assert.Null(delayed.NextAttemptAt),
            () => Assert.Null(delayed.LeaseToken));
        Assert.Equal("Approved recipe", (await database.Recipes.AsNoTracking().SingleAsync(recipe => recipe.Id == live.Id)).Name);
        await photoPromotion.DidNotReceive().PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());
    }

    [RequiresDockerTheory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task PromotionStopsWhenTheClaimRunsOutOfTimeAsync(int availableSeconds) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        var live = Recipe.Create(author.Id, "Approved recipe", servings: 2);
        var photo = ImageAsset.Create(author.Id, "moderation/timeout.jpg", "https://cdn.example.com/timeout.jpg");
        database.AddRange(live, photo);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        IRecipeImagePromotionService photoPromotion = Substitute.For<IRecipeImagePromotionService>();
        photoPromotion.PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>())
            .Returns(call => WaitForPromotionCancellationAsync(call.Arg<CancellationToken>()));
        var claimedAt = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(claimedAt);
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi,
            photoPromotion: photoPromotion, privateAssets: [photo], clock: clock);
        Guid revisionId = GetRevisionId(await ReplacePendingAsync(provider, author.Id, live.Id.Value, "Revised recipe", [photo.Id.Value]));
        Assert.True(await ProcessAsync(provider));

        // Simulate time elapsing while the approval claim is persisted. Promotion reserves the final 30 seconds for publication.
        DateTimeOffset promotionStartsAt = claimedAt.AddMinutes(5).AddSeconds(-30 - availableSeconds);
        clock.GetUtcNow().Returns(claimedAt, promotionStartsAt);

        using var testDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Assert.True(await ProcessAsync(provider, cancellationToken: testDeadline.Token));

        RecipePublicationRevision approval = await ReadRevisionAsync(database, revisionId);
        Assert.Equal(RecipePublicationStatuses.Approved, approval.Status);
        Assert.Equal("Approved recipe", (await database.Recipes.AsNoTracking().SingleAsync(recipe => recipe.Id == live.Id, testDeadline.Token)).Name);
        if (availableSeconds == 0) {
            await photoPromotion.DidNotReceive().PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());
            Assert.Equal(claimedAt.AddMinutes(5), approval.LeaseUntil);
            Assert.Null(approval.Reason);
        } else {
            await photoPromotion.Received(1).PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());
            DateTimeOffset retriesAt = Assert.IsType<DateTimeOffset>(approval.NextAttemptAt);
            Assert.Multiple(
                () => Assert.Equal(RecipePublicationStatuses.PhotoPromotionUnavailable, approval.Reason),
                () => Assert.Null(approval.LeaseToken),
                () => Assert.InRange(retriesAt, promotionStartsAt.AddSeconds(10), promotionStartsAt.AddSeconds(14)));

            clock.GetUtcNow().Returns(retriesAt);
            photoPromotion.PromoteAsync(photo.Id, author.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());
            Assert.True(await ProcessAsync(provider, cancellationToken: testDeadline.Token));

            RecipePublicationRevision published = await ReadRevisionAsync(database, revisionId);
            Assert.Multiple(
                () => Assert.Equal(RecipePublicationStatuses.Published, published.Status),
                () => Assert.Null(published.Reason),
                () => Assert.Equal(approval.PrimaryResult, published.PrimaryResult));
            Assert.Equal("Revised recipe", (await database.Recipes.AsNoTracking()
                .SingleAsync(recipe => recipe.Id == live.Id, testDeadline.Token)).Name);
        }
    }

    private static async Task<Result> WaitForPromotionCancellationAsync(CancellationToken cancellationToken) {
        var completion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using CancellationTokenRegistration registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return await completion.Task;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ModerationClock(DateTimeOffset now) : TimeProvider {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
