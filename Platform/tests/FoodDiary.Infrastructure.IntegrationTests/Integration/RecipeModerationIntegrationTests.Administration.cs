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
    public async Task AdministrationPaginatesHistoryAndCountsOnlyCurrentRevisionsAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-queue@example.com", "hash");
        database.Users.Add(author);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<RecipeModerationCheck>(new RecipeModerationProviderException("Fixture failure.", retryable: false)),
            _ => Task.FromResult(Check(flagged: true)),
            _ => Task.FromResult(Check(flagged: false)));
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, clock: clock);

        Guid delayed = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        Assert.True(await ProcessAsync(provider));
        clock.Advance(TimeSpan.FromMinutes(1));
        Guid review = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        Assert.True(await ProcessAsync(provider));
        clock.Advance(TimeSpan.FromMinutes(1));
        Guid approved = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        Assert.True(await ProcessAsync(provider));
        clock.Advance(TimeSpan.FromMinutes(1));
        RecipeModel older = await CreatePendingAsync(provider, author.Id);
        Guid superseded = GetRevisionId(older);
        clock.Advance(TimeSpan.FromMinutes(1));
        Guid pending = GetRevisionId(await ReplacePendingAsync(provider, author.Id, older.Id, "Revised queue recipe"));

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();
        RecipeModerationPage all = await moderation.ListAsync(string.Empty, page: 0, limit: 101, CancellationToken.None);
        RecipeModerationPage secondPending = await moderation.ListAsync(RecipePublicationStatuses.Pending, page: 2, limit: 1, CancellationToken.None);
        RecipeModerationPage needingReview = await moderation.ListAsync(RecipePublicationStatuses.NeedsReview, page: 1, limit: 0, CancellationToken.None);
        RecipeModerationItem approval = Assert.Single(secondPending.Items);
        RecipeModerationItem reviewItem = Assert.Single(needingReview.Items);

        Assert.Multiple(
            () => Assert.Equal(new[] { pending, superseded, approved, review, delayed }, all.Items.Select(item => item.RevisionId)),
            () => Assert.Equal(5, all.TotalItems),
            () => Assert.Equal(1, all.Page),
            () => Assert.Equal(100, all.Limit),
            () => Assert.Equal(2, all.PendingCount),
            () => Assert.Equal(1, all.NeedsReviewCount),
            () => Assert.Equal(1, all.DelayedCount),
            () => Assert.Equal(2, secondPending.TotalItems),
            () => Assert.Equal(approved, approval.RevisionId),
            () => Assert.Equal(RecipePublicationStatuses.Pending, approval.Status),
            () => Assert.Equal(review, reviewItem.RevisionId),
            () => Assert.Equal(1, needingReview.Limit));

        Assert.Equal(RecipeModerationActionOutcome.Applied,
            await moderation.RetryAsync(delayed, author.Id.Value, CancellationToken.None));
        RecipePublicationRevision retried = await database.Set<RecipePublicationRevision>().AsNoTracking().SingleAsync(item => item.Id == delayed);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, retried.Status),
            () => Assert.Equal(0, retried.Attempts),
            () => Assert.Null(retried.Reason),
            () => Assert.Equal(author.Id.Value, retried.ReviewedBy),
            () => Assert.Equal(clock.GetUtcNow(), retried.NextAttemptAt));
    }

    [RequiresDockerFact]
    public async Task AdministrationRequiresAnActionableRevisionAndPersistsTheTrimmedReviewReasonAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-decision@example.com", "hash");
        database.Users.Add(author);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: true));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);
        Guid revisionId = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        var actor = Guid.NewGuid();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();

        Assert.Equal(RecipeModerationActionOutcome.NotFound,
            await moderation.ResolveAsync(Guid.NewGuid(), actor, approve: true, reason: null, CancellationToken.None));
        Assert.Equal(RecipeModerationActionOutcome.Conflict,
            await moderation.ResolveAsync(revisionId, actor, approve: true, reason: null, CancellationToken.None));
        Assert.True(await ProcessAsync(provider));
        await Assert.ThrowsAsync<ArgumentException>("reason", () => moderation.ResolveAsync(
            revisionId, actor, approve: false, reason: " ", CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>("reason", () => moderation.ResolveAsync(
            revisionId, actor, approve: true, new string('x', 2001), CancellationToken.None));

        Assert.Equal(RecipeModerationActionOutcome.Applied, await moderation.ResolveAsync(
            revisionId, actor, approve: false, reason: "  Replace the recipe text.  ", CancellationToken.None));
        RecipePublicationRevision resolved = await database.Set<RecipePublicationRevision>().AsNoTracking().SingleAsync(item => item.Id == revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.ChangesRequested, resolved.Status),
            () => Assert.Equal("Replace the recipe text.", resolved.Reason),
            () => Assert.Equal(actor, resolved.ReviewedBy),
            () => Assert.Null(resolved.NextAttemptAt));
    }

    [RequiresDockerFact]
    public async Task AdministrationPreviewsOnlyImagesReferencedByTheRevisionAndOwnedByItsAuthorAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-preview@example.com", "hash");
        var recipe = Recipe.Create(author.Id, "Original soup", servings: 2);
        recipe.AddStep(1, "Cook.");
        var photo = ImageAsset.Create(author.Id, "moderation/preview.jpg", "https://cdn.example.com/preview.jpg");
        database.AddRange(author, recipe, photo);
        await database.SaveChangesAsync();
        const string dataUrl = "data:image/jpeg;base64,Zm9vZA==";
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        images.GetDataUrlAsync(photo.Id, author.Id, Arg.Any<CancellationToken>()).Returns(
            Result.Success(dataUrl), Result.Failure<string>(new Error("Images.FixtureUnavailable", "Private storage detail.")));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, CreateProviderStub("OpenAi"),
            privateAssets: [photo], imageContent: images);
        Guid revisionId = GetRevisionId(await ReplacePendingAsync(provider, author.Id, recipe.Id.Value, "Revised soup", [photo.Id.Value]));
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();

        Assert.Null(await moderation.GetImageAsync(Guid.NewGuid(), photo.Id.Value, CancellationToken.None));
        Assert.Null(await moderation.GetImageAsync(revisionId, Guid.NewGuid(), CancellationToken.None));
        Assert.Empty(images.ReceivedCalls());
        Assert.Equal(dataUrl, await moderation.GetImageAsync(revisionId, photo.Id.Value, CancellationToken.None));
        Assert.Null(await moderation.GetImageAsync(revisionId, photo.Id.Value, CancellationToken.None));
        await images.Received(2).GetDataUrlAsync(photo.Id, author.Id, Arg.Any<CancellationToken>());

        RecipeModerationItem item = await GetItemAsync(provider, revisionId);
        Assert.NotNull(item.Images);
        Assert.Equal(new RecipeModerationPreviewImage(photo.Id.Value, "recipe/photo/1"), Assert.Single(item.Images));
    }

    [RequiresDockerFact]
    public async Task AdministrationValidatesPolicyCategoriesAndRestoresHistoryAsANewVersionAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, CreateProviderStub("OpenAi"), clock: clock);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();
        var actor = Guid.NewGuid();
        var rules = new RecipeModerationPolicyInput(["violence", "hate", "violence"], ["violence", "sexual", "violence"], 0);

        await Assert.ThrowsAsync<ArgumentException>("input", () => moderation.PublishPolicyAsync(
            actor, rules with { MistralSamplePercent = -1 }, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>("input", () => moderation.PublishPolicyAsync(
            actor, rules with { MistralSamplePercent = 101 }, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>("categories", () => moderation.PublishPolicyAsync(
            actor, rules with { ImageReviewCategories = ["hate"] }, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>("categories", () => moderation.PublishPolicyAsync(
            actor, rules with { TextReviewCategories = [] }, CancellationToken.None));
        Assert.Null(await moderation.RestorePolicyAsync(Guid.NewGuid(), actor, CancellationToken.None));
        RecipeModerationPolicyVersion defaultVersion = Assert.Single((await moderation.GetPolicyAsync(CancellationToken.None)).History);

        RecipeModerationPolicyView first = await moderation.PublishPolicyAsync(actor, rules, CancellationToken.None);
        RecipeModerationPolicyView second = await moderation.PublishPolicyAsync(
            actor, rules with { TextReviewCategories = ["harassment"], MistralSamplePercent = 100 }, CancellationToken.None);
        var restorer = Guid.NewGuid();
        RecipeModerationPolicyView? restored = await moderation.RestorePolicyAsync(first.Active.Id, restorer, CancellationToken.None);
        Assert.NotNull(restored);

        Assert.Multiple(
            () => Assert.Equal(new[] { "hate", "violence" }, first.Active.Rules.TextReviewCategories),
            () => Assert.Equal(new[] { "sexual", "violence" }, first.Active.Rules.ImageReviewCategories),
            () => Assert.Equal(100, second.Active.Rules.MistralSamplePercent),
            () => Assert.Equal(first.Active.Rules.TextReviewCategories, restored.Active.Rules.TextReviewCategories),
            () => Assert.Equal(first.Active.Rules.ImageReviewCategories, restored.Active.Rules.ImageReviewCategories),
            () => Assert.Equal(0, restored.Active.Rules.MistralSamplePercent),
            () => Assert.Equal(restorer, restored.Active.PublishedBy),
            () => Assert.True(restored.Active.PublishedAt > second.Active.PublishedAt),
            () => Assert.Equal(new[] { restored.Active.Id, second.Active.Id, first.Active.Id, defaultVersion.Id },
                restored.History.Select(version => version.Id)));
    }
}
