using Microsoft.Extensions.Options;
using Npgsql;
using MsOptions = Microsoft.Extensions.Options.Options;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Authentication;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramAssertionReplayGuardIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();
    private const string SignedAssertionFingerprint = "v2:c607fb633d9d1db2231549d49b42d6f88efa1fbe9d769f111938d8e0198c313c";

    [RequiresDockerFact]
    public async Task ReplayPolicy_RejectsPreUpgradeProofsAndLegacyWriterButAllowsFreshProof() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        await context.Set<TelegramAssertionPolicy>().ExecuteUpdateAsync(update => update.SetProperty(policy => policy.NotBeforeUtc, UtcNow));
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);
        Assert.False(await guard.TryConsumeAsync("canonical-old-proof", UtcNow.AddDays(1), UtcNow.AddSeconds(-1)));
        Assert.Empty(await context.ConsumedTelegramAssertions.ToArrayAsync());
        string legacyFingerprint = new('d', 64);
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"ConsumedTelegramAssertions\" (\"Fingerprint\", \"ExpiresAtUtc\") VALUES ({legacyFingerprint}, {UtcNow.AddDays(1)})"));
        Assert.True(await guard.TryConsumeAsync("fresh-proof", UtcNow.AddDays(1), UtcNow));
    }

    [RequiresDockerFact]
    public async Task EquivalentSignedAssertions_CannotBeConsumedAcrossContextsOrAfterOneDay() {
        const string botToken = "test-only-bot-token";
        const string user = "{\"id\":42,\"first_name\":\"Alex User\"}";
        long authDate = new DateTimeOffset(UtcNow).ToUnixTimeSeconds();
        string dataCheck = string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}\nuser={user}");
        byte[] secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        string hash = Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(dataCheck)));
        string raw = string.Create(CultureInfo.InvariantCulture, $"user={Uri.EscapeDataString(user)}&auth_date={authDate}&hash={hash}");
        string alternate = string.Join('&', raw.Split('&').Reverse()).Replace("%20", "+", StringComparison.Ordinal);
        IOptions<TelegramAuthOptions> options = MsOptions.Create(new TelegramAuthOptions { BotToken = botToken, AuthTtlSeconds = 172800 });
        var validator = new TelegramAuthValidator(options, FixedTime);
        TelegramInitData first = validator.ValidateInitData(raw).Value;
        TelegramInitData second = validator.ValidateInitData(alternate).Value;
        Assert.Equal(first.ReplayIdentity, second.ReplayIdentity);
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        await using IdentityDbContext anotherContext = new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(context.Database.GetConnectionString()).Options);
        Assert.True(await new TelegramAssertionReplayGuard(context, FixedTime).TryConsumeAsync(first.ReplayIdentity, first.ExpiresAtUtc, first.AuthDateUtc));
        var later = new AdvancedTimeProvider();
        Assert.True(new TelegramAuthValidator(options, later).ValidateInitData(alternate).IsSuccess);
        Assert.False(await new TelegramAssertionReplayGuard(anotherContext, later).TryConsumeAsync(second.ReplayIdentity, second.ExpiresAtUtc, second.AuthDateUtc));
    }

    [RequiresDockerFact]
    public async Task TelegramAssertionReplayGuard_ConsumesAssertionOnlyOnceAndDeletesExpiredRows() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);

        bool first = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow);
        bool duplicate = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow);
        bool expired = await guard.TryConsumeAsync("expired-assertion", UtcNow, UtcNow);
        bool afterCleanup = await guard.TryConsumeAsync("another-assertion", UtcNow.AddMinutes(5), UtcNow);

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
        await using IdentityDbContext setup = await IdentityContextTestFactory.CreateAsync(databaseFixture);
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
            await using IdentityDbContext context = IdentityContextTestFactory.Create(connectionString);
            var guard = new TelegramAssertionReplayGuard(context, FixedTime);
            if (Interlocked.Increment(ref readyCount) == 4) {
                ready.SetResult();
            }

            await start.Task.WaitAsync(cancellationToken);
            return await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow, cancellationToken);
        }
    }

    [RequiresDockerFact]
    public async Task TryConsumeAsync_RemovesExpiredAndBoundaryRowsButPreservesUnexpiredRows() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        string expiredFingerprint = "v2:" + new string('a', 64);
        string boundaryFingerprint = "v2:" + new string('b', 64);
        string unexpiredFingerprint = "v2:" + new string('c', 64);
        context.Set<ConsumedTelegramAssertion>().AddRange(
            new ConsumedTelegramAssertion { Fingerprint = expiredFingerprint, ExpiresAtUtc = UtcNow.AddSeconds(-1) },
            new ConsumedTelegramAssertion { Fingerprint = boundaryFingerprint, ExpiresAtUtc = UtcNow },
            new ConsumedTelegramAssertion { Fingerprint = unexpiredFingerprint, ExpiresAtUtc = UtcNow.AddSeconds(1) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);

        Assert.True(await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow));

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
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow, cancellation.Token));

        Assert.Empty(await context.Set<ConsumedTelegramAssertion>().ToArrayAsync());
        Assert.True(await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5), UtcNow));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AdvancedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow.AddHours(25), TimeSpan.Zero);
    }
}
