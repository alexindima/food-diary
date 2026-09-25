using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
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
        Assert.Equal("Ai.RecognitionConflict", (await store.CreateAsync(job with { AdditionalImages = [] }, CancellationToken.None)).Error.Code);
        Assert.Equal("Ai.RecognitionConflict", (await store.CreateAsync(job with { IsProductLabel = false }, CancellationToken.None)).Error.Code);
        FoodRecognitionJobModel loaded = Assert.IsType<FoodRecognitionJobModel>(await store.GetAsync(job.UserId, job.Id, CancellationToken.None));
        Assert.True(loaded.IsProductLabel);
        Assert.Equal(labelPhoto.Id.Value, Assert.Single(loaded.AdditionalImages!).ImageAssetId);
        Assert.Single(Assert.Single((await store.ListAsync(job.UserId, 1, 20, isProductLabel: null, CancellationToken.None)).Items).AdditionalImages!);
        FoodRecognitionJobModel claimed = Assert.IsType<FoodRecognitionJobModel>(await store.ClaimAsync(CancellationToken.None));
        Assert.Single(claimed.AdditionalImages!);
        var label = new ProductLabelModel("Yogurt", Brand: null, 100, "g", 63, 5, 2, 6, Fiber: null, Alcohol: null, Notes: null);
        Assert.True(await store.SaveVisionAsync(job.Id, new FoodVisionModel([], ProductLabel: label), CancellationToken.None));
        await store.CompleteAsync(job.Id, nutrition: null, errorCode: null, nutritionErrorCode: null, CancellationToken.None);
        Assert.Equal(label, (await store.GetAsync(job.UserId, job.Id, CancellationToken.None))!.Vision!.ProductLabel);
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
        Assert.Single((await store.ListAsync(job.UserId, 1, 20, isProductLabel: null, CancellationToken.None)).Items);
        Assert.Null(await store.GetAsync(Guid.NewGuid(), job.Id, CancellationToken.None));
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
        Assert.True(await store.SaveVisionAsync(job.Id, vision, CancellationToken.None));
        clock.Now = clock.Now.AddMinutes(6);
        await store.MaintainAsync(CancellationToken.None);
        await store.CompleteAsync(job.Id, nutrition: null, errorCode: null, nutritionErrorCode: null, CancellationToken.None);
        FoodRecognitionJobModel? interrupted = await store.GetAsync(job.UserId, job.Id, CancellationToken.None);
        Assert.NotNull(interrupted);
        Assert.Equal("Failed", interrupted.Status);
        Assert.Equal("Ai.RecognitionInterrupted", interrupted.ErrorCode);
        Assert.Equal("Apple", Assert.Single(interrupted.Vision!.Items).NameEn);
        Assert.Null(await store.ClaimAsync(CancellationToken.None));
        Assert.False(await store.SaveVisionAsync(job.Id, new FoodVisionModel([]), CancellationToken.None));
        await using var context = new FoodDiaryDbContext(new DbContextOptions<FoodDiaryDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.ImageAssets.ExecuteDeleteAsync());
        clock.Now = clock.Now.AddDays(8);
        await store.MaintainAsync(CancellationToken.None);
        Assert.Null(await store.GetAsync(job.UserId, job.Id, CancellationToken.None));
        Assert.Equal(1, await context.ImageAssets.ExecuteDeleteAsync());
    }

    [RequiresDockerTheory]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    public async Task DeleteCompleted_IsOwnerScopedAndPreservesImageAssets(string status) {
        (DbContextOptions<AiDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        await store.CreateAsync(job, CancellationToken.None);
        Assert.Equal("Ai.RecognitionInProgress", (await store.DeleteCompletedAsync(job.UserId, job.Id, CancellationToken.None)).Error.Code);
        await store.ClaimAsync(CancellationToken.None);
        Assert.Equal("Ai.RecognitionInProgress", (await store.DeleteCompletedAsync(job.UserId, job.Id, CancellationToken.None)).Error.Code);
        await store.CompleteAsync(job.Id, nutrition: null, string.Equals(status, "Failed", StringComparison.Ordinal) ? "Ai.OpenAiFailed" : null, nutritionErrorCode: null, CancellationToken.None);
        Assert.Equal("Ai.RecognitionNotFound", (await store.DeleteCompletedAsync(Guid.NewGuid(), job.Id, CancellationToken.None)).Error.Code);
        Assert.NotNull(await store.GetAsync(job.UserId, job.Id, CancellationToken.None));
        Assert.True((await store.DeleteCompletedAsync(job.UserId, job.Id, CancellationToken.None)).IsSuccess);
        Assert.Null(await store.GetAsync(job.UserId, job.Id, CancellationToken.None));
        Assert.Equal("Ai.RecognitionNotFound", (await store.DeleteCompletedAsync(job.UserId, job.Id, CancellationToken.None)).Error.Code);
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

        (IReadOnlyList<FoodRecognitionJobModel> firstItems, int firstTotal) = await store.ListAsync(job.UserId, 1, 20, isProductLabel: true, CancellationToken.None);
        (IReadOnlyList<FoodRecognitionJobModel> secondItems, int _) = await store.ListAsync(job.UserId, 2, 20, isProductLabel: true, CancellationToken.None);
        Assert.Equal(23, firstTotal);
        Assert.Equal(20, firstItems.Count);
        Assert.Equal(3, secondItems.Count);
        Assert.Empty(firstItems.Select(item => item.Id).Intersect(secondItems.Select(item => item.Id)));
        Assert.Equal(firstItems.Select(item => item.Id), (await store.ListAsync(job.UserId, 1, 20, isProductLabel: true, CancellationToken.None)).Items.Select(item => item.Id));
        Assert.Equal(2, (await store.ListAsync(job.UserId, 1, 20, isProductLabel: false, CancellationToken.None)).TotalItems);
        Assert.Equal(0, (await store.ListAsync(Guid.NewGuid(), 1, 20, isProductLabel: true, CancellationToken.None)).TotalItems);
        (IReadOnlyList<FoodRecognitionJobModel> beyondItems, int beyondTotal) = await store.ListAsync(job.UserId, 3, 20, isProductLabel: true, CancellationToken.None);
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
