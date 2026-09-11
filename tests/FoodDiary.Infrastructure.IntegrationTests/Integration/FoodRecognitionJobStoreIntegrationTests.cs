using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Ai;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionJobStoreIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ConcurrentAdmissionsAndClaims_AreIdempotentAndOwnerScoped() {
        (DbContextOptions<FoodDiaryDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Result<FoodRecognitionJobModel>[] admissions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => store.CreateAsync(job, CancellationToken.None)));
        Assert.All(admissions, result => Assert.True(result.IsSuccess));
        Assert.Single(await store.ListAsync(job.UserId, CancellationToken.None));
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
        (DbContextOptions<FoodDiaryDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
        var store = new FoodRecognitionJobStore(options, TimeProvider.System);
        Result<FoodRecognitionJobModel>[] admissions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => store.CreateAsync(job with { Id = Guid.NewGuid() }, CancellationToken.None)));
        Assert.Equal(2, admissions.Count(result => result.IsSuccess));
        Assert.All(admissions.Where(result => result.IsFailure), result => Assert.Equal("Ai.RecognitionQueueFull", result.Error.Code));
    }

    [RequiresDockerFact]
    public async Task InterruptedTask_PreservesCheckpointRejectsLateCompletionAndEventuallyReleasesImage() {
        (DbContextOptions<FoodDiaryDbContext> options, FoodRecognitionJobModel job) = await CreateDatabaseAsync();
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
        await using var context = new FoodDiaryDbContext(options);
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.ImageAssets.ExecuteDeleteAsync());
        clock.Now = clock.Now.AddDays(8);
        await store.MaintainAsync(CancellationToken.None);
        Assert.Null(await store.GetAsync(job.UserId, job.Id, CancellationToken.None));
        Assert.Equal(1, await context.ImageAssets.ExecuteDeleteAsync());
    }

    private async Task<(DbContextOptions<FoodDiaryDbContext>, FoodRecognitionJobModel)> CreateDatabaseAsync() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString, provider => provider.EnableRetryOnFailure()).Options;
        await using var context = new FoodDiaryDbContext(options);
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
