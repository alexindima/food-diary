using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramAssertionReplayGuardIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();
    private const string SignedAssertionFingerprint = "c607fb633d9d1db2231549d49b42d6f88efa1fbe9d769f111938d8e0198c313c";

    [RequiresDockerFact]
    public async Task TelegramAssertionReplayGuard_ConsumesAssertionOnlyOnceAndDeletesExpiredRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);

        bool first = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5));
        bool duplicate = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5));
        bool expired = await guard.TryConsumeAsync("expired-assertion", UtcNow);
        bool afterCleanup = await guard.TryConsumeAsync("another-assertion", UtcNow.AddMinutes(5));

        Assert.Multiple(
            () => Assert.True(first),
            () => Assert.False(duplicate),
            () => Assert.True(expired),
            () => Assert.True(afterCleanup));

        ConsumedTelegramAssertion[] rows = await context.Set<ConsumedTelegramAssertion>().AsNoTracking().ToArrayAsync();
        ConsumedTelegramAssertion stored = Assert.Single(rows, row => string.Equals(row.Fingerprint, SignedAssertionFingerprint, StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.Equal(2, rows.Length),
            () => Assert.Equal(UtcNow.AddMinutes(5), stored.ExpiresAtUtc),
            () => Assert.All(rows, row => Assert.True(row.ExpiresAtUtc > UtcNow)));
    }

    [RequiresDockerFact]
    public async Task TryConsumeAsync_WithConcurrentContexts_OnlyOneAttemptSucceeds() {
        await using FoodDiaryDbContext setup = await databaseFixture.CreateDbContextAsync();
        string connectionString = setup.Database.GetConnectionString()!;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        int readyCount = 0;
        Task<bool>[] attempts = [.. Enumerable.Range(0, 4).Select(_ => ConsumeAsync(deadline.Token))];

        await ready.Task.WaitAsync(deadline.Token);
        start.SetResult();
        bool[] results = await Task.WhenAll(attempts).WaitAsync(deadline.Token);

        Assert.Multiple(
            () => Assert.Equal(1, results.Count(result => result)),
            () => Assert.Equal(3, results.Count(result => !result)));
        ConsumedTelegramAssertion stored = Assert.Single(await setup.Set<ConsumedTelegramAssertion>().AsNoTracking().ToArrayAsync());
        Assert.Multiple(
            () => Assert.Equal(SignedAssertionFingerprint, stored.Fingerprint),
            () => Assert.Equal(UtcNow.AddMinutes(5), stored.ExpiresAtUtc));

        async Task<bool> ConsumeAsync(CancellationToken cancellationToken) {
            await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
            var guard = new TelegramAssertionReplayGuard(context, FixedTime);
            if (Interlocked.Increment(ref readyCount) == 4) {
                ready.SetResult();
            }

            await start.Task.WaitAsync(cancellationToken);
            return await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), cancellationToken);
        }
    }

    [RequiresDockerFact]
    public async Task TryConsumeAsync_RemovesExpiredAndBoundaryRowsButPreservesUnexpiredRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        string expiredFingerprint = new('a', 64);
        string boundaryFingerprint = new('b', 64);
        string unexpiredFingerprint = new('c', 64);
        context.Set<ConsumedTelegramAssertion>().AddRange(
            new ConsumedTelegramAssertion { Fingerprint = expiredFingerprint, ExpiresAtUtc = UtcNow.AddSeconds(-1) },
            new ConsumedTelegramAssertion { Fingerprint = boundaryFingerprint, ExpiresAtUtc = UtcNow },
            new ConsumedTelegramAssertion { Fingerprint = unexpiredFingerprint, ExpiresAtUtc = UtcNow.AddSeconds(1) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);

        Assert.True(await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5)));

        string[] fingerprints = await context.Set<ConsumedTelegramAssertion>().Select(row => row.Fingerprint).ToArrayAsync();
        Assert.Multiple(
            () => Assert.Equal(2, fingerprints.Length),
            () => Assert.Contains(unexpiredFingerprint, fingerprints, StringComparer.Ordinal),
            () => Assert.Contains(SignedAssertionFingerprint, fingerprints, StringComparer.Ordinal),
            () => Assert.DoesNotContain(expiredFingerprint, fingerprints, StringComparer.Ordinal),
            () => Assert.DoesNotContain(boundaryFingerprint, fingerprints, StringComparer.Ordinal));
    }

    [RequiresDockerFact]
    public async Task TryConsumeAsync_WithCancelledToken_DoesNotConsumeAssertion() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), cancellation.Token));

        Assert.Empty(await context.Set<ConsumedTelegramAssertion>().ToArrayAsync());
        Assert.True(await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
