using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using FoodDiary.ReadModel.Composition.Images;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionJobStoreIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData("Succeeded", true, null, null)]
    [InlineData("Failed", false, "Ai.RecognitionInterrupted", null)]
    [InlineData("Succeeded", false, null, "Ai.InvalidResponse")]
    public async Task SavedCheckpointAndErrorFields_RoundTripThroughAFreshStoreAsync(
        string status, bool hasNutrition, string? errorCode, string? nutritionErrorCode) {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Assert.True((await store.CreateAsync(job, CancellationToken.None)).IsSuccess);
        var created = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        DateTime updated = created.AddMinutes(3);
        const string visionJson = """
            {"items":[{"nameEn":"Apple","nameLocal":null,"amount":125.5,"unit":"g","confidence":0.875}],"notes":"saved vision"}
            """;
        const string nutritionJson = """
            {"calories":66.375,"protein":0.375,"fat":0.25,"carbs":17.625,"fiber":3.125,"alcohol":0,
             "items":[{"name":"Apple","amount":125.5,"unit":"g","calories":66.375,"protein":0.375,"fat":0.25,"carbs":17.625,"fiber":3.125,"alcohol":0}],
             "notes":"saved nutrition"}
            """;
        await using (var context = new AiDbContext(options)) {
            FoodRecognitionJob saved = await context.Set<FoodRecognitionJob>().SingleAsync(row => row.Id == job.Id);
            saved.Status = status;
            saved.CreatedOnUtc = created;
            saved.UpdatedOnUtc = updated;
            saved.VisionJson = visionJson;
            saved.NutritionJson = hasNutrition ? nutritionJson : null;
            saved.ErrorCode = errorCode;
            saved.NutritionErrorCode = nutritionErrorCode;
            await context.SaveChangesAsync();
        }

        FoodRecognitionJobModel restored = Assert.IsType<FoodRecognitionJobModel>(await store.GetAsync(
            new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(job.Id, restored.Id),
            () => Assert.Equal(job.UserId, restored.UserId),
            () => Assert.Equal(job.ImageAssetId, restored.ImageAssetId),
            () => Assert.Equal(status, restored.Status),
            () => Assert.Equal(created, restored.CreatedOnUtc),
            () => Assert.Equal(updated, restored.UpdatedOnUtc),
            () => Assert.Equal(DateTimeKind.Utc, restored.UpdatedOnUtc.Kind),
            () => Assert.Equal(errorCode, restored.ErrorCode),
            () => Assert.Equal(nutritionErrorCode, restored.NutritionErrorCode));
        FoodVisionModel vision = Assert.IsType<FoodVisionModel>(restored.Vision);
        FoodVisionItemModel item = Assert.Single(vision.Items);
        Assert.Multiple(
            () => Assert.Equal("saved vision", vision.Notes),
            () => Assert.Equal("Apple", item.NameEn),
            () => Assert.Null(item.NameLocal),
            () => Assert.Equal(125.5m, item.Amount),
            () => Assert.Equal("g", item.Unit),
            () => Assert.Equal(0.875m, item.Confidence));
        if (hasNutrition) {
            FoodNutritionModel nutrition = Assert.IsType<FoodNutritionModel>(restored.Nutrition);
            FoodNutritionItemModel nutritionItem = Assert.Single(nutrition.Items);
            Assert.Multiple(
                () => Assert.Equal("saved nutrition", nutrition.Notes),
                () => Assert.Equal(66.375m, nutrition.Calories),
                () => Assert.Equal(0.375m, nutrition.Protein),
                () => Assert.Equal(0.25m, nutrition.Fat),
                () => Assert.Equal(17.625m, nutrition.Carbs),
                () => Assert.Equal(3.125m, nutrition.Fiber),
                () => Assert.Equal(0m, nutrition.Alcohol),
                () => Assert.Equal(new FoodNutritionItemModel("Apple", 125.5m, "g", 66.375m, 0.375m, 0.25m, 17.625m, 3.125m, 0m), nutritionItem));
        } else {
            Assert.Null(restored.Nutrition);
        }
    }

    [RequiresDockerFact]
    public async Task ProductPhotos_RoundTripProtectCleanupAndParticipateInIdempotency() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        await using var context = new FoodDiaryDbContext(new DbContextOptions<FoodDiaryDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        ImageAsset primary = await context.ImageAssets.SingleAsync();
        var labelPhoto = ImageAsset.Create(primary.UserId, "label.jpg", "https://example.com/label.jpg");
        context.ImageAssets.Add(labelPhoto);
        await context.SaveChangesAsync();
        job = job with { IsProductLabel = true, AdditionalImages = [new FoodRecognitionImageModel(labelPhoto.Id.Value, "https://example.com/label.jpg")] };
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Assert.True((await store.CreateAsync(job, CancellationToken.None)).IsSuccess);
        Assert.True((await store.CreateAsync(job, CancellationToken.None)).IsSuccess);
        FoodRecognitionJobImage persistedLabel = await context.Set<FoodRecognitionJobImage>().AsNoTracking()
            .SingleAsync(image => image.ImageAssetId == labelPhoto.Id);
        Assert.Equal(job.Id, persistedLabel.JobId);
        Assert.Equal(labelPhoto.Id, persistedLabel.ImageAssetId);
        Assert.Equal("Ai.RecognitionConflict", (await store.CreateAsync(job with { AdditionalImages = [] }, CancellationToken.None)).Error.Code);
        Assert.Equal("Ai.RecognitionConflict", (await store.CreateAsync(job with { IsProductLabel = false }, CancellationToken.None)).Error.Code);
        FoodRecognitionJobModel loaded = Assert.IsType<FoodRecognitionJobModel>(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        Assert.True(loaded.IsProductLabel);
        Assert.Equal(labelPhoto.Id.Value, Assert.Single(loaded.AdditionalImages!).ImageAssetId);
        Assert.Single(Assert.Single((await store.ListAsync(new UserId(job.UserId), 1, 20, isProductLabel: null, CancellationToken.None)).Items).AdditionalImages!);
        FoodRecognitionJobModel claimed = Assert.IsType<FoodRecognitionJobModel>(await store.ClaimAsync(CancellationToken.None));
        Assert.Single(claimed.AdditionalImages!);
        var label = new ProductLabelModel("Yogurt", Brand: null, 100, "g", 63, 5, 2, 6, Fiber: null, Alcohol: null, Notes: null);
        Assert.True(await store.SaveVisionAsync(new FoodRecognitionJobId(job.Id), new FoodVisionModel([], ProductLabel: label), CancellationToken.None));
        await store.CompleteAsync(new FoodRecognitionJobId(job.Id), FoodRecognitionCompletion.WithoutNutrition, CancellationToken.None);
        Assert.Equal(label, (await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None))!.Vision!.ProductLabel);
        var usage = new ImageAssetUsageQuery(context);
        Assert.True(await usage.IsAssetInUseAsync(labelPhoto.Id));
        await context.FoodRecognitionJobs.ExecuteDeleteAsync();
        Assert.False(await usage.IsAssetInUseAsync(labelPhoto.Id));
    }

    [RequiresDockerTheory]
    [InlineData("Queued")]
    [InlineData("Running")]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    public async Task ReferencedImage_IsProtectedFromCleanup_UntilRecognitionJobIsDeleted(string status) {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Assert.True((await store.CreateAsync(job, CancellationToken.None)).IsSuccess);
        await using var context = new FoodDiaryDbContext(new DbContextOptions<FoodDiaryDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        await context.FoodRecognitionJobs.ExecuteUpdateAsync(set => set.SetProperty(item => item.Status, status));
        ImageAsset referenced = await context.ImageAssets.SingleAsync();
        var unused = ImageAsset.Create(referenced.UserId, "unused/image.jpg", "https://example.com/unused.jpg");
        context.ImageAssets.Add(unused);
        await context.SaveChangesAsync();
        var query = new ImageAssetUsageQuery(context);
        DateTime cutoff = DateTime.UtcNow.AddDays(1);

        Assert.True(await query.IsAssetInUseAsync(referenced.Id));
        Assert.False(await query.IsAssetInUseAsync(unused.Id));
        Assert.Equal(unused.Id, Assert.Single(await query.GetUnusedCandidatesOlderThanAsync(cutoff, 10)).Id);

        await context.FoodRecognitionJobs.ExecuteDeleteAsync();

        Assert.False(await query.IsAssetInUseAsync(referenced.Id));
        Assert.Contains(await query.GetUnusedCandidatesOlderThanAsync(cutoff, 10), candidate => candidate.Id == referenced.Id);
    }

    [RequiresDockerFact]
    public async Task ConcurrentAdmissionsAndClaims_AreIdempotentAndOwnerScoped() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Result<FoodRecognitionJobModel>[] admissions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => store.CreateAsync(job, CancellationToken.None)));
        Assert.All(admissions, result => Assert.True(result.IsSuccess));
        Assert.Single((await store.ListAsync(new UserId(job.UserId), 1, 20, isProductLabel: null, CancellationToken.None)).Items);
        Assert.Null(await store.GetAsync(new UserId(Guid.NewGuid()), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        Result<FoodRecognitionJobModel> conflict = await store.CreateAsync(job with { Description = "changed" }, CancellationToken.None);
        Assert.Equal("Ai.RecognitionConflict", conflict.Error.Code);
        FoodRecognitionJobModel?[] claims = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => store.ClaimAsync(CancellationToken.None)));
        Assert.Single(claims, result => result is not null);
        Assert.Null(await store.ClaimAsync(CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task ConcurrentDistinctAdmissions_EnforceTwoOutstandingTasks() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Result<FoodRecognitionJobModel>[] admissions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => store.CreateAsync(job with { Id = Guid.NewGuid() }, CancellationToken.None)));
        Assert.Equal(2, admissions.Count(result => result.IsSuccess));
        Assert.All(admissions.Where(result => result.IsFailure), result => Assert.Equal("Ai.RecognitionQueueFull", result.Error.Code));
    }

    [RequiresDockerFact]
    public async Task InterruptedTask_PreservesCheckpointRejectsLateCompletionAndEventuallyReleasesImage() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var store = new FoodRecognitionJobStore(options, clock);
        await store.CreateAsync(job, CancellationToken.None);
        await store.ClaimAsync(CancellationToken.None);
        var vision = new FoodVisionModel([new FoodVisionItemModel("Apple", NameLocal: null, 100, "g", 1)]);
        Assert.True(await store.SaveVisionAsync(new FoodRecognitionJobId(job.Id), vision, CancellationToken.None));
        clock.Now = clock.Now.AddMinutes(6);
        await store.MaintainAsync(CancellationToken.None);
        await store.CompleteAsync(new FoodRecognitionJobId(job.Id), FoodRecognitionCompletion.WithoutNutrition, CancellationToken.None);
        FoodRecognitionJobModel? interrupted = await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None);
        Assert.NotNull(interrupted);
        Assert.Equal("Failed", interrupted.Status);
        Assert.Equal("Ai.RecognitionInterrupted", interrupted.ErrorCode);
        Assert.Equal("Apple", Assert.Single(interrupted.Vision!.Items).NameEn);
        Assert.Null(await store.ClaimAsync(CancellationToken.None));
        Assert.False(await store.SaveVisionAsync(new FoodRecognitionJobId(job.Id), new FoodVisionModel([]), CancellationToken.None));
        await using var context = new FoodDiaryDbContext(new DbContextOptions<FoodDiaryDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.ImageAssets.ExecuteDeleteAsync());
        clock.Now = clock.Now.AddDays(8);
        await store.MaintainAsync(CancellationToken.None);
        Assert.Null(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        Assert.Equal(1, await context.ImageAssets.ExecuteDeleteAsync());
    }

    [RequiresDockerFact]
    public async Task NutritionFailure_PreservesCheckpointAndRejectsLateSuccess() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        await store.CreateAsync(job, CancellationToken.None);
        await store.ClaimAsync(CancellationToken.None);
        var vision = new FoodVisionModel([new FoodVisionItemModel("Apple", NameLocal: null, 0.125m, "pcs", 1)]);
        Assert.True(await store.SaveVisionAsync(new FoodRecognitionJobId(job.Id), vision, CancellationToken.None));
        await store.CompleteAsync(new FoodRecognitionJobId(job.Id), FoodRecognitionCompletion.NutritionFailed("Ai.InvalidResponse"), CancellationToken.None);
        FoodRecognitionJobModel partial = Assert.IsType<FoodRecognitionJobModel>(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        FoodVisionItemModel partialItem = Assert.Single(Assert.IsType<FoodVisionModel>(partial.Vision).Items);
        Assert.Multiple(
            () => Assert.Equal("Succeeded", partial.Status),
            () => Assert.Null(partial.Nutrition),
            () => Assert.Null(partial.ErrorCode),
            () => Assert.Equal("Ai.InvalidResponse", partial.NutritionErrorCode),
            () => Assert.Equal(0.125m, partialItem.Amount));
        Assert.IsType<FoodRecognitionResult.Unusable>(FoodRecognitionResult.Decode(partial));
        Assert.Null(await store.ClaimAsync(CancellationToken.None));
        Assert.False(await store.SaveVisionAsync(new FoodRecognitionJobId(job.Id), new FoodVisionModel([]), CancellationToken.None));
        var nutrition = new FoodNutritionModel(52.125m, 0, 0, 0, 0, 0, [new FoodNutritionItemModel("Apple", 0.125m, "pcs", 52.125m, 0, 0, 0, 0, 0)]);
        await store.CompleteAsync(new FoodRecognitionJobId(job.Id), FoodRecognitionCompletion.WithNutrition(nutrition), CancellationToken.None);
        FoodRecognitionJobModel unchanged = Assert.IsType<FoodRecognitionJobModel>(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        FoodVisionItemModel unchangedItem = Assert.Single(Assert.IsType<FoodVisionModel>(unchanged.Vision).Items);
        Assert.Multiple(
            () => Assert.Equal("Ai.InvalidResponse", unchanged.NutritionErrorCode),
            () => Assert.Null(unchanged.Nutrition),
            () => Assert.Equal(0.125m, unchangedItem.Amount));
    }

    [RequiresDockerTheory]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    public async Task DeleteCompleted_IsOwnerScopedAndPreservesImageAssets(string status) {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        await store.CreateAsync(job, CancellationToken.None);
        Assert.Equal("Ai.RecognitionInProgress", (await store.DeleteCompletedAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None)).Error.Code);
        await store.ClaimAsync(CancellationToken.None);
        Assert.Equal("Ai.RecognitionInProgress", (await store.DeleteCompletedAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None)).Error.Code);
        await store.CompleteAsync(new FoodRecognitionJobId(job.Id), string.Equals(status, "Failed", StringComparison.Ordinal) ? FoodRecognitionCompletion.VisionFailed("Ai.OpenAiFailed") : FoodRecognitionCompletion.WithoutNutrition, CancellationToken.None);
        Assert.Equal("Ai.RecognitionNotFound", (await store.DeleteCompletedAsync(new UserId(Guid.NewGuid()), new FoodRecognitionJobId(job.Id), CancellationToken.None)).Error.Code);
        Assert.NotNull(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        Assert.True((await store.DeleteCompletedAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None)).IsSuccess);
        Assert.Null(await store.GetAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None));
        Assert.Equal("Ai.RecognitionNotFound", (await store.DeleteCompletedAsync(new UserId(job.UserId), new FoodRecognitionJobId(job.Id), CancellationToken.None)).Error.Code);
        await using var context = new FoodDiaryDbContext(new DbContextOptions<FoodDiaryDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        Assert.Equal(job.ImageAssetId, (await context.ImageAssets.SingleAsync()).Id.Value);
    }

    [RequiresDockerFact]
    public async Task List_PaginatesFilteredRecentOwnedJobsWithStableOrdering() {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var store = new FoodRecognitionJobStore(options, new TestClock(now));
        await using var context = new AiDbContext(options);
        for (int index = 0; index < 25; index++) {
            context.FoodRecognitionJobs.Add(new FoodDiary.Modules.Ai.PersistenceModel.FoodRecognitionJob {
                Id = Guid.NewGuid(),
                UserId = new UserId(job.UserId),
                ImageAssetId = new ImageAssetId(job.ImageAssetId),
                ImageUrl = job.ImageUrl,
                Status = index % 2 == 0 ? "Succeeded" : "Failed",
                IsProductLabel = index < 23,
                CreatedOnUtc = now.UtcDateTime,
                UpdatedOnUtc = now.UtcDateTime,
            });
        }
        context.FoodRecognitionJobs.Add(new FoodDiary.Modules.Ai.PersistenceModel.FoodRecognitionJob {
            Id = Guid.NewGuid(),
            UserId = new UserId(job.UserId),
            ImageAssetId = new ImageAssetId(job.ImageAssetId),
            ImageUrl = job.ImageUrl,
            Status = "Succeeded",
            IsProductLabel = true,
            CreatedOnUtc = now.AddDays(-8).UtcDateTime,
            UpdatedOnUtc = now.UtcDateTime,
        });
        await context.SaveChangesAsync();

        (IReadOnlyList<FoodRecognitionJobModel> firstItems, int firstTotal) = await store.ListAsync(new UserId(job.UserId), 1, 20, isProductLabel: true, CancellationToken.None);
        (IReadOnlyList<FoodRecognitionJobModel> secondItems, int _) = await store.ListAsync(new UserId(job.UserId), 2, 20, isProductLabel: true, CancellationToken.None);
        Assert.Equal(23, firstTotal);
        Assert.Equal(20, firstItems.Count);
        Assert.Equal(3, secondItems.Count);
        Assert.Empty(firstItems.Select(item => item.Id).Intersect(secondItems.Select(item => item.Id)));
        Assert.Equal(firstItems.Select(item => item.Id), (await store.ListAsync(new UserId(job.UserId), 1, 20, isProductLabel: true, CancellationToken.None)).Items.Select(item => item.Id));
        Assert.Equal(2, (await store.ListAsync(new UserId(job.UserId), 1, 20, isProductLabel: false, CancellationToken.None)).TotalItems);
        Assert.Equal(0, (await store.ListAsync(new UserId(Guid.NewGuid()), 1, 20, isProductLabel: true, CancellationToken.None)).TotalItems);
        (IReadOnlyList<FoodRecognitionJobModel> beyondItems, int beyondTotal) = await store.ListAsync(new UserId(job.UserId), 3, 20, isProductLabel: true, CancellationToken.None);
        Assert.Empty(beyondItems);
        Assert.Equal(23, beyondTotal);
    }

    private async Task<(DbContextOptions<AiDbContext>, FoodRecognitionJobModel)> CreateDatabaseAsync() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        DbContextOptions<AiDbContext> options = new DbContextOptionsBuilder<AiDbContext>()
            .UseNpgsql(connectionString, provider => provider.EnableRetryOnFailure()).Options;
        await using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString).Options);
        await context.Database.MigrateAsync();
        Assert.False(context.Database.HasPendingModelChanges());
        var user = User.Create($"recognition-{Guid.NewGuid():N}@example.com", "hash");
        var image = ImageAsset.Create(user.Id, "recognition/image.jpg", "https://example.com/image.jpg");
        image.Confirm();
        context.Users.Add(user);
        context.ImageAssets.Add(image);
        await context.SaveChangesAsync();
        var job = new FoodRecognitionJobModel(Guid.NewGuid(), user.Id.Value, image.Id.Value, image.Url,
            Description: null, "Queued", DateTime.UtcNow, DateTime.UtcNow);
        return (options, job);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestClock(DateTimeOffset now) : TimeProvider {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
