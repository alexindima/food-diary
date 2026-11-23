using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class RecipeModerationIntegrationTests {
    [RequiresDockerFact]
    public async Task ProcessingSwitchPreservesSubmissionUntilWorkerIsEnabledAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);
        IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
        configuration["RecipeModeration:ProcessingEnabled"] = "false";
        RecipeModel pending = await CreatePendingAsync(provider, author.Id);
        Guid revisionId = GetRevisionId(pending);

        Assert.False(await ProcessAsync(provider));

        RecipePublicationRevision paused = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, paused.Status),
            () => Assert.Equal(0, paused.Attempts),
            () => Assert.Null(paused.LeaseToken));
        await openAi.DidNotReceive().CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());

        configuration["RecipeModeration:ProcessingEnabled"] = "true";

        Assert.True(await ProcessAsync(provider));
        await openAi.Received(1).CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        Assert.Equal(RecipePublicationStatuses.Approved, (await ReadRevisionAsync(database, revisionId)).Status);
    }

    [RequiresDockerFact]
    public async Task UnavailableProvidersStopTheirOwnQueueWithoutRecordingResultsAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.IsAvailable.Returns(returnThis: false);
        IRecipeModerationProvider mistral = CreateProviderStub("Mistral");
        mistral.IsAvailable.Returns(returnThis: false);
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, mistral);
        await PublishPolicyAsync(provider, author.Id, "harassment");
        Guid revisionId = GetRevisionId(await CreatePendingAsync(provider, author.Id));

        Assert.True(await ProcessAsync(provider, observation: true));

        RecipePublicationRevision observed = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, observed.Status),
            () => Assert.Null(observed.ShadowResult),
            () => Assert.Null(observed.ShadowNextAttemptAt),
            () => Assert.Null(observed.ShadowLeaseToken));
        Assert.False(await ProcessAsync(provider, observation: true));

        Assert.True(await ProcessAsync(provider));

        RecipePublicationRevision delayed = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.CheckDelayed, delayed.Status),
            () => Assert.Equal("ProviderUnavailable", delayed.Reason),
            () => Assert.Null(delayed.PrimaryResult),
            () => Assert.Null(delayed.NextAttemptAt),
            () => Assert.Null(delayed.LeaseToken));
        Assert.False(await ProcessAsync(provider));
        await openAi.DidNotReceive().CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        await mistral.DidNotReceive().CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
    }

    [RequiresDockerFact]
    public async Task RetryAfterAndLocalBackoffRequeueBeforeAttemptsAreExhaustedAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        var retryAfter = TimeSpan.FromMinutes(2);
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<RecipeModerationCheck>(new RecipeModerationProviderException(
                "Synthetic provider slowdown.", retryable: true, retryAfter)));
        IRecipeModerationProvider mistral = CreateProviderStub("Mistral");
        mistral.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<RecipeModerationCheck>(new RecipeModerationProviderException(
                "Synthetic observation slowdown.", retryable: true, retryAfter: TimeSpan.FromSeconds(1))));
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, mistral, clock: clock);
        await PublishPolicyAsync(provider, author.Id, "harassment");
        Guid revisionId = GetRevisionId(await CreatePendingAsync(provider, author.Id));

        Assert.True(await ProcessAsync(provider));
        Assert.True(await ProcessAsync(provider, observation: true));

        RecipePublicationRevision retry = await ReadRevisionAsync(database, revisionId);
        DateTimeOffset nextShadowAttempt = Assert.IsType<DateTimeOffset>(retry.ShadowNextAttemptAt);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, retry.Status),
            () => Assert.Equal(1, retry.Attempts),
            () => Assert.Equal(1, retry.ShadowAttempts),
            () => Assert.Equal(clock.GetUtcNow() + retryAfter, retry.NextAttemptAt),
            () => Assert.InRange(nextShadowAttempt, clock.GetUtcNow().AddSeconds(10), clock.GetUtcNow().AddSeconds(14)),
            () => Assert.Null(retry.LeaseToken),
            () => Assert.Null(retry.ShadowLeaseToken));
        Assert.False(await ProcessAsync(provider));
        Assert.False(await ProcessAsync(provider, observation: true));

        const int maximumAttempts = 6;
        for (int attempt = 2; attempt <= maximumAttempts; attempt++) {
            clock.Advance(TimeSpan.FromHours(1));
            Assert.True(await ProcessAsync(provider));
            Assert.True(await ProcessAsync(provider, observation: true));
        }

        RecipePublicationRevision exhausted = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.CheckDelayed, exhausted.Status),
            () => Assert.Equal("CheckUnavailable", exhausted.Reason),
            () => Assert.Equal(maximumAttempts, exhausted.Attempts),
            () => Assert.Equal(maximumAttempts, exhausted.ShadowAttempts),
            () => Assert.Null(exhausted.NextAttemptAt),
            () => Assert.Null(exhausted.ShadowNextAttemptAt));
        Assert.False(await ProcessAsync(provider));
        Assert.False(await ProcessAsync(provider, observation: true));
    }

    [RequiresDockerFact]
    public async Task ProviderRejectionRequiringAnImageChangeStopsAutomaticAttemptsAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        var recipe = Recipe.Create(author.Id, "Private soup", servings: 2, visibility: Visibility.Private);
        var photo = ImageAsset.Create(author.Id, "moderation/rejected.jpg", "https://cdn.example.com/rejected.jpg");
        database.AddRange(recipe, photo);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Is<RecipeModerationContent>(content => content.Images.Any(image => image.AssetId == photo.Id.Value)),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<RecipeModerationCheck>(new RecipeModerationProviderException(
                "Synthetic invalid photo.", retryable: false, requiresContentChanges: true)));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, privateAssets: [photo]);
        Guid revisionId = GetRevisionId(await ReplacePendingAsync(provider, author.Id, recipe.Id.Value, "Photo soup", [photo.Id.Value]));

        Assert.True(await ProcessAsync(provider));

        RecipePublicationRevision rejected = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.ChangesRequested, rejected.Status),
            () => Assert.Equal("ReplaceImage", rejected.Reason),
            () => Assert.Null(rejected.NextAttemptAt),
            () => Assert.Null(rejected.PrimaryResult));
        Assert.False(await ProcessAsync(provider));
    }

    [RequiresDockerFact]
    public async Task InvalidApprovedDraftStopsPublicationAndReturnsToReviewAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        var reader = User.Create("moderation-reader@example.com", "hash");
        database.Users.Add(reader);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);
        RecipeModel pending = await CreatePendingAsync(provider, author.Id);
        Guid revisionId = GetRevisionId(pending);
        await database.Set<RecipePublicationRevision>().Where(revision => revision.Id == revisionId)
            .ExecuteUpdateAsync(update => update.SetProperty(revision => revision.ChangeJson, "null"));

        Assert.True(await ProcessAsync(provider));
        Assert.True(await ProcessAsync(provider));

        RecipePublicationRevision review = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.NeedsReview, review.Status),
            () => Assert.Equal("UnableToApply", review.Reason),
            () => Assert.NotNull(review.PrimaryResult),
            () => Assert.Null(review.NextAttemptAt),
            () => Assert.Null(review.LeaseToken));
        Assert.Null(await ReadRecipeAsync(provider, new RecipeId(pending.Id), reader.Id));
        Assert.False(await ProcessAsync(provider));
    }

    private static async Task<User> CreateModerationAuthorAsync(FoodDiaryDbContext database) {
        var author = User.Create("moderation-author@example.com", "hash");
        database.Users.Add(author);
        await database.SaveChangesAsync();
        return author;
    }

    private static Task<RecipePublicationRevision> ReadRevisionAsync(FoodDiaryDbContext database, Guid revisionId) =>
        database.Set<RecipePublicationRevision>().AsNoTracking().SingleAsync(revision => revision.Id == revisionId);
}
