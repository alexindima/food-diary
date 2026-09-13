using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramLoginTicketStoreIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [RequiresDockerFact]
    public async Task InvalidParameters_DoNotPersistTickets() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var store = new TelegramLoginTicketStore(context, new EphemeralDataProtectionProvider(), new FixedClock());
        await Assert.ThrowsAsync<ArgumentException>(() => store.CreateAsync("login", "browser", "payload", Now, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => store.CreateAsync("login", "browser", new string('я', 4097), Now.AddMinutes(1), CancellationToken.None));
        Assert.Null(await store.ConsumeAsync("invalid-ticket", "login", "browser", CancellationToken.None));
        Assert.Empty(await context.Set<TelegramLoginTicket>().ToArrayAsync());
    }

    [RequiresDockerFact]
    public async Task WrongProtectionKey_ConsumesUnrecoverableTicketWithoutExposingPayload() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var original = new TelegramLoginTicketStore(context, new EphemeralDataProtectionProvider(), new FixedClock());
        string ticket = await original.CreateAsync("login", "browser", "identity", Now.AddMinutes(5), CancellationToken.None);
        var differentKey = new TelegramLoginTicketStore(context, new EphemeralDataProtectionProvider(), new FixedClock());
        Assert.Null(await differentKey.ConsumeAsync(ticket, "login", "browser", CancellationToken.None));
        Assert.Null(await original.ConsumeAsync(ticket, "login", "browser", CancellationToken.None));
        Assert.Empty(await context.Set<TelegramLoginTicket>().ToArrayAsync());
    }

    [RequiresDockerFact]
    public async Task Ticket_IsEncryptedBoundAndSingleUse() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var protection = new EphemeralDataProtectionProvider();
        var store = new TelegramLoginTicketStore(context, protection, new FixedClock());
        string ticket = await store.CreateAsync("onboarding", "browser-one", "private-identity", Now.AddMinutes(5), CancellationToken.None);
        TelegramLoginTicket record = Assert.Single(await context.Set<TelegramLoginTicket>().AsNoTracking().ToArrayAsync());
        Assert.DoesNotContain("private-identity", record.ProtectedPayload, StringComparison.Ordinal);
        Assert.NotEqual(ticket, record.Fingerprint, StringComparer.Ordinal);
        Assert.Null(await store.ConsumeAsync(ticket, "link", "browser-one", CancellationToken.None));
        Assert.Null(await store.ConsumeAsync(ticket, "onboarding", "browser-two", CancellationToken.None));
        Assert.Equal("private-identity", await store.ConsumeAsync(ticket, "onboarding", "browser-one", CancellationToken.None));
        Assert.Null(await store.ConsumeAsync(ticket, "onboarding", "browser-one", CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task ConcurrentConsumers_OnlyOneReceivesPayload() {
        await using IdentityDbContext setup = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        string connectionString = setup.Database.GetConnectionString()!;
        var protection = new EphemeralDataProtectionProvider();
        var store = new TelegramLoginTicketStore(setup, protection, new FixedClock());
        string ticket = await store.CreateAsync("login", "browser", "identity", Now.AddMinutes(5), CancellationToken.None);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<string?>[] attempts = [.. Enumerable.Range(0, 4).Select(_ => ConsumeAsync())];
        start.SetResult();
        string?[] results = await Task.WhenAll(attempts).WaitAsync(deadline.Token);
        Assert.Single(results, result => string.Equals(result, "identity", StringComparison.Ordinal));
        Assert.Equal(3, results.Count(result => result is null));

        async Task<string?> ConsumeAsync() {
            await using IdentityDbContext context = IdentityContextTestFactory.Create(connectionString);
            await start.Task.WaitAsync(deadline.Token);
            return await new TelegramLoginTicketStore(context, protection, new FixedClock())
                .ConsumeAsync(ticket, "login", "browser", deadline.Token);
        }
    }

    [RequiresDockerFact]
    public async Task ExpiryBoundary_RejectsTicket() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        var protection = new EphemeralDataProtectionProvider();
        var clock = new FixedClock();
        var store = new TelegramLoginTicketStore(context, protection, clock);
        string ticket = await store.CreateAsync("login", "browser", "identity", Now.AddMinutes(5), CancellationToken.None);
        clock.NowUtc = Now.AddMinutes(5);
        Assert.Null(await store.ConsumeAsync(ticket, "login", "browser", CancellationToken.None));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public DateTime NowUtc { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() => new(NowUtc);
    }
}
