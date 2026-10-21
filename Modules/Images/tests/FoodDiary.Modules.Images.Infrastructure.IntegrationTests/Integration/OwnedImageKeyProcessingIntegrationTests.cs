using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Infrastructure.Persistence;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FoodDiary.Modules.Images.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class OwnedImageKeyProcessingIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ExplicitKeys_ProcessOnlyPendingRequestedRowsAndPreserveStorageVariantsAndOtherQueueEntries() {
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        const string key = "users/test-owner/images/requested.webp";
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var confirmed = ImageObjectDeletionOutboxMessage.Create(key, isConfirmed: true, now.AddMinutes(-5));
        var staging = ImageObjectDeletionOutboxMessage.Create(key, isConfirmed: false, now.AddMinutes(-4));
        var unrelated = ImageObjectDeletionOutboxMessage.Create("users/test-other/images/unrelated.webp", now.AddMinutes(-3));
        var processed = ImageObjectDeletionOutboxMessage.Create(key, now.AddMinutes(-2));
        processed.MarkProcessed(now.AddMinutes(-1));
        var dead = ImageObjectDeletionOutboxMessage.Create(key, now.AddMinutes(-2));
        dead.MarkDeadLettered("held for review", now.AddMinutes(-1));
        database.AddRange(confirmed, staging, unrelated, processed, dead);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        IImageStorageService storage = Substitute.For<IImageStorageService>();
        storage.DeleteAsync(Arg.Any<ObjectStorageKey>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton<IModuleContextFactory>(database);
        services.AddSingleton<IModuleScopeGuard>(database);
        services.AddSingleton(storage);
        services.AddSingleton<TimeProvider>(new FixedClock(now));
        services.AddOptions<OutboxProcessingOptions>();
        services.AddLogging();
        services.AddImagesInfrastructure();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IImageObjectDeletionOutboxProcessor processor = provider.GetRequiredService<IImageObjectDeletionOutboxProcessor>();
        using var cancellation = new CancellationTokenSource();

        int count = await processor.ProcessKeysAsync([key, key, "users/test-owner/images/missing.webp"], cancellation.Token);

        Assert.Equal(2, count);
        await storage.Received(1).DeleteAsync(Arg.Is<ObjectStorageKey>(value => string.Equals(value.Value, key, StringComparison.Ordinal)), isConfirmed: true, Arg.Any<CancellationToken>());
        await storage.Received(1).DeleteAsync(Arg.Is<ObjectStorageKey>(value => string.Equals(value.Value, key, StringComparison.Ordinal)), isConfirmed: false, Arg.Any<CancellationToken>());
        Assert.Equal(2, storage.ReceivedCalls().Count());
        ImagesDbContext owned = provider.GetRequiredService<ImagesDbContext>();
        Dictionary<Guid, ImageObjectDeletionOutboxMessage> rows = await owned.ImageObjectDeletionOutbox.AsNoTracking().ToDictionaryAsync(row => row.Id, cancellation.Token);
        Assert.Multiple(
            () => Assert.Equal(now, rows[confirmed.Id].ProcessedOnUtc),
            () => Assert.Equal(now, rows[staging.Id].ProcessedOnUtc),
            () => Assert.Null(rows[unrelated.Id].ProcessedOnUtc),
            () => Assert.Equal(now.AddMinutes(-1), rows[processed.Id].ProcessedOnUtc),
            () => Assert.Null(rows[dead.Id].ProcessedOnUtc),
            () => Assert.Equal(now.AddMinutes(-1), rows[dead.Id].DeadLetteredOnUtc),
            () => Assert.Equal("held for review", rows[dead.Id].LastError),
            () => Assert.Empty(owned.ChangeTracker.Entries()));
        Assert.Equal(0, await processor.ProcessKeysAsync([key], cancellation.Token));
        Assert.Equal(2, storage.ReceivedCalls().Count());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
