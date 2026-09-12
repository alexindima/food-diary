using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramOperationStoreIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [RequiresDockerFact]
    public async Task MissingProtectionKey_PreservesWorkAndAllowsRecoveryAfterKeyRestoration() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var clock = new Clock();
        var originalProtection = new EphemeralDataProtectionProvider();
        var originalStore = new TelegramOperationStore(context, originalProtection, clock);
        Guid? original = await originalStore.RegisterAsync(123, 10, Guid.NewGuid(), 1, "original-photo", CancellationToken.None);
        Assert.NotNull(original);
        var replacementStore = new TelegramOperationStore(context, new EphemeralDataProtectionProvider(), clock);
        Guid? other = await replacementStore.RegisterAsync(123, 11, Guid.NewGuid(), 1, "other-photo", CancellationToken.None);
        Assert.NotNull(other);

        await Assert.ThrowsAsync<CryptographicException>(() => replacementStore.AcquireAsync(123, original.Value, CancellationToken.None));

        TelegramOperation stored = await context.Set<TelegramOperation>().AsNoTracking().SingleAsync(item => item.Id == original.Value);
        Assert.False(stored.Completed);
        Assert.NotEmpty(stored.ProtectedPayload);
        Assert.DoesNotContain(original.Value, await replacementStore.ListReadyAsync(123, CancellationToken.None));
        TelegramOperationLease? otherLease = await replacementStore.AcquireAsync(123, other.Value, CancellationToken.None);
        Assert.NotNull(otherLease);
        Assert.Equal("other-photo", otherLease.Payload);

        clock.UtcNow = stored.LeaseExpiresAtUtc!.Value;
        TelegramOperationLease? restored = await originalStore.AcquireAsync(123, original.Value, CancellationToken.None);
        Assert.NotNull(restored);
        Assert.Equal(original.Value, restored.OperationId);
        Assert.Equal("original-photo", restored.Payload);
    }

    [RequiresDockerFact]
    public async Task Registration_DeduplicatesAndRejectsChangedPayloadOrBinding() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var store = new TelegramOperationStore(context, new EphemeralDataProtectionProvider(), new Clock());
        var userId = Guid.NewGuid();
        Guid? operation = await store.RegisterAsync(123, 10, userId, 1, "private-photo-reference", CancellationToken.None);
        Assert.NotNull(operation);
        Assert.Equal(operation, await store.RegisterAsync(123, 10, userId, 1, "private-photo-reference", CancellationToken.None));
        Assert.Null(await store.RegisterAsync(123, 10, userId, 1, "changed", CancellationToken.None));
        Assert.Null(await store.RegisterAsync(123, 10, userId, 2, "private-photo-reference", CancellationToken.None));
        TelegramOperation stored = Assert.Single(await context.Set<TelegramOperation>().AsNoTracking().ToListAsync());
        Assert.DoesNotContain("private-photo-reference", stored.ProtectedPayload, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task Lease_RecoveryRejectsStaleWriterAndCompletedOperationCannotRestart() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var clock = new Clock();
        var store = new TelegramOperationStore(context, new EphemeralDataProtectionProvider(), clock);
        Guid? id = await store.RegisterAsync(123, 10, Guid.NewGuid(), 1, "payload", CancellationToken.None);
        Assert.NotNull(id);
        TelegramOperationLease? first = await store.AcquireAsync(123, id.Value, CancellationToken.None);
        Assert.NotNull(first);
        Assert.Equal(Now, first.CreatedAtUtc);
        Assert.Null(await store.AcquireAsync(123, id.Value, CancellationToken.None));
        clock.UtcNow = first.LeaseExpiresAtUtc;
        TelegramOperationLease? recovered = await store.AcquireAsync(123, id.Value, CancellationToken.None);
        Assert.NotNull(recovered);
        Assert.Equal(first.CreatedAtUtc, recovered.CreatedAtUtc);
        Assert.False(await store.CheckpointAsync(123, id.Value, first.LeaseId, "stale", completed: false, clock.UtcNow, CancellationToken.None));
        Assert.True(await store.CheckpointAsync(123, id.Value, recovered.LeaseId, "meal-saved", completed: true, clock.UtcNow, CancellationToken.None));
        Assert.Null(await store.AcquireAsync(123, id.Value, CancellationToken.None));
        Assert.Empty(await store.ListReadyAsync(123, CancellationToken.None));
        TelegramOperation stored = await context.Set<TelegramOperation>().AsNoTracking().SingleAsync();
        Assert.Empty(stored.ProtectedPayload);
        Assert.Null(stored.ProtectedCheckpoint);
    }

    [RequiresDockerFact]
    public async Task TerminalCleanup_ErasesOldContentButPreservesDeduplicationAndPendingWork() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var store = new TelegramOperationStore(context, new EphemeralDataProtectionProvider(), new Clock());
        var userId = Guid.NewGuid();
        Guid? done = await store.RegisterAsync(123, 10, userId, 1, "original-payload", CancellationToken.None);
        Guid? pending = await store.RegisterAsync(123, 11, userId, 1, "pending-payload", CancellationToken.None);
        Assert.NotNull(done);
        Assert.NotNull(pending);
        TelegramOperationLease? lease = await store.AcquireAsync(123, done.Value, CancellationToken.None);
        Assert.NotNull(lease);
        Assert.True(await store.CheckpointAsync(123, done.Value, lease.LeaseId, "private-checkpoint", completed: true, Now, CancellationToken.None));
        await context.Set<TelegramOperation>().Where(item => item.Id == done.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ProtectedCheckpoint, "legacy-encrypted-content"));

        IReadOnlyList<Guid> ready = await store.ListReadyAsync(123, CancellationToken.None);

        Assert.Equal(pending.Value, Assert.Single(ready));
        TelegramOperation record = await context.Set<TelegramOperation>().AsNoTracking().SingleAsync(item => item.Id == done.Value);
        Assert.Empty(record.ProtectedPayload);
        Assert.Null(record.ProtectedCheckpoint);
        Assert.Equal(done, await store.RegisterAsync(123, 10, userId, 1, "original-payload", CancellationToken.None));
        Assert.Null(await store.AcquireAsync(123, done.Value, CancellationToken.None));
        TelegramOperationLease? pendingLease = await store.AcquireAsync(123, pending.Value, CancellationToken.None);
        Assert.NotNull(pendingLease);
        Assert.Equal("pending-payload", pendingLease.Payload);
    }

    [RequiresDockerFact]
    public async Task ConcurrentWorkers_OnlyOneAcquiresAndCancellationFencesItsWrites() {
        await using FoodDiaryDbContext setup = await databaseFixture.CreateDbContextAsync();
        string connection = setup.Database.GetConnectionString()!;
        var protection = new EphemeralDataProtectionProvider();
        var userId = Guid.NewGuid();
        var store = new TelegramOperationStore(setup, protection, new Clock());
        Guid? id = await store.RegisterAsync(123, 10, userId, 1, "payload", CancellationToken.None);
        Assert.NotNull(id);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<TelegramOperationLease?>[] attempts = [.. Enumerable.Range(0, 4).Select(_ => AcquireAsync())];
        start.SetResult();
        TelegramOperationLease?[] results = await Task.WhenAll(attempts);
        TelegramOperationLease lease = Assert.Single(results.OfType<TelegramOperationLease>());
        await store.CancelUserAsync(userId, CancellationToken.None);
        Assert.False(await store.CheckpointAsync(123, id.Value, lease.LeaseId, "after-disconnect", completed: false, Now, CancellationToken.None));

        async Task<TelegramOperationLease?> AcquireAsync() {
            await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connection);
            await start.Task;
            return await new TelegramOperationStore(context, protection, new Clock()).AcquireAsync(123, id.Value, CancellationToken.None);
        }
    }

    private sealed class Clock : TimeProvider {
        public DateTime UtcNow { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }
}
