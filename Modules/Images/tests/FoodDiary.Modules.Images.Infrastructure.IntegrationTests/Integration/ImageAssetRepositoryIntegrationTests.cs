using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Images.Application.Abstractions.Models;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.ReadModel.Composition.Images;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

namespace FoodDiary.Modules.Images.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ImageAssetRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CleanupCursor_PreservesEqualTimestampRowsAfterPreviousPageDeletion() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("image-cursor@example.com", "hash");
        ImageAsset[] assets = [.. Enumerable.Range(0, 3).Select(i => ImageAsset.Create(user.Id, FormattableString.Invariant($"cursor/{i}"), FormattableString.Invariant($"https://cdn.example.com/{i}")))];
        DateTime created = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Users.Add(user);
        context.ImageAssets.AddRange(assets);
        foreach (ImageAsset asset in assets) {
            context.Entry(asset).Property(item => item.CreatedOnUtc).CurrentValue = created;
        }
        await context.SaveChangesAsync();
        var query = new ImageAssetUsageQuery(context);
        IReadOnlyList<ImageCleanupCandidate> first = await query.GetUnusedCandidatesOlderThanAsync(created.AddDays(1), 2);
        Assert.Equal(2, first.Count);
        context.ImageAssets.Remove(assets.Single(asset => asset.Id == first[0].Id));
        await context.SaveChangesAsync();

        IReadOnlyList<ImageCleanupCandidate> next = await query.GetUnusedCandidatesOlderThanAsync(created.AddDays(1), 2, first[^1]);

        ImageCleanupCandidate last = Assert.Single(next);
        Assert.DoesNotContain(first, candidate => candidate.Id == last.Id);
        Assert.Equal(assets.OrderBy(asset => asset.Id.Value).Last().Id, last.Id);
        Assert.Empty(await query.GetUnusedCandidatesOlderThanAsync(created.AddDays(1), 2, last));
    }

    [RequiresDockerFact]
    public async Task OwnedLookup_HidesForeignAsset_AndPersistsConfirmation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("image-owner@example.com", "hash");
        var otherUser = User.Create("image-other@example.com", "hash");
        var asset = ImageAsset.Create(owner.Id, "images/pending.webp", "https://cdn.example.com/pending.webp");
        context.Users.AddRange(owner, otherUser);
        context.ImageAssets.Add(asset);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ImageAssetRepository(context.ImageAssets, new ImageAssetUsageQuery(context));

        ImageAsset? foreign = await repository.GetOwnedByIdAsync(asset.Id, otherUser.Id, CancellationToken.None);
        ImageAsset? owned = await repository.GetOwnedForUpdateAsync(asset.Id, owner.Id, CancellationToken.None);
        Assert.Null(foreign);
        Assert.NotNull(owned);
        Assert.False(owned.IsConfirmed);

        owned.Confirm();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ImageAsset? confirmed = await repository.GetOwnedByIdAsync(asset.Id, owner.Id, CancellationToken.None);
        Assert.NotNull(confirmed);
        Assert.True(confirmed.IsConfirmed);
    }

    [RequiresDockerFact]
    public async Task MealAiSessionAsset_IsReportedInUse_AndExcludedFromUnusedCandidates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("image-ai@example.com", "hash");
        var referencedAsset = ImageAsset.Create(user.Id, "images/ai-referenced.webp", "https://cdn.example.com/ai-referenced.webp");
        var unusedAsset = ImageAsset.Create(user.Id, "images/unused.webp", "https://cdn.example.com/unused.webp");

        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc));
        meal.AddAiSession(
            referencedAsset.Id,
            AiRecognitionSource.Photo,
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            "analysis",
            []);

        context.Users.Add(user);
        context.ImageAssets.AddRange(referencedAsset, unusedAsset);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new ImageAssetRepository(context.ImageAssets, new ImageAssetUsageQuery(context));

        bool isReferencedAssetInUse = await repository.IsAssetInUseAsync(referencedAsset.Id, CancellationToken.None);
        IReadOnlyList<ImageCleanupCandidate> unusedCandidates = await new ImageAssetUsageQuery(context).GetUnusedCandidatesOlderThanAsync(
            DateTime.UtcNow.AddDays(1),
            batchSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.True(isReferencedAssetInUse);
        Assert.DoesNotContain(unusedCandidates, asset => asset.Id == referencedAsset.Id);
        Assert.Contains(unusedCandidates, asset => asset.Id == unusedAsset.Id);
    }
}
