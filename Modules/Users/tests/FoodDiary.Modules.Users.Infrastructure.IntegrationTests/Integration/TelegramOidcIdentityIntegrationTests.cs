using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using FoodDiary.Infrastructure.Persistence.Users;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramOidcIdentityIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SaveBoundary_TranslatesRealTelegramUniqueViolation() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        seed.Users.Add(User.CreateTelegram(782005, "hash"));
        await seed.SaveChangesAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString())
            .AddInterceptors(new TelegramIdentityConflictInterceptor())
            .Options;
        await using var contender = new FoodDiaryDbContext(options);
        contender.Users.Add(User.CreateTelegram(782005, "other-hash"));

        DbUpdateConcurrencyException conflict = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contender.SaveChangesAsync());

        Assert.Null(conflict.InnerException);
        Assert.Equal(1, await seed.Users.CountAsync(user => user.TelegramUserId == 782005));
    }

    [RequiresDockerFact]
    public async Task ConcurrentTelegramAccounts_PersistExactlyOneOwner() {
        await using FoodDiaryDbContext first = await databaseFixture.CreateDbContextAsync();
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(first.Database.GetConnectionString()!);
        first.Users.Add(User.CreateTelegram(782004, "first-hash"));
        second.Users.Add(User.CreateTelegram(782004, "second-hash"));

        bool[] committed = await Task.WhenAll(TrySaveTelegramAccountAsync(first), TrySaveTelegramAccountAsync(second));

        Assert.Single(committed, value => value);
        first.ChangeTracker.Clear();
        User owner = Assert.Single(await first.Users.Where(user => user.TelegramUserId == 782004).ToListAsync());
        Assert.Null(owner.Email);
        Assert.False(owner.HasPassword);
    }

    private static async Task<bool> TrySaveTelegramAccountAsync(FoodDiaryDbContext context) {
        try {
            await context.SaveChangesAsync();
            return true;
        } catch (DbUpdateException exception) {
            PostgresException postgres = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("IX_Users_TelegramUserId", postgres.ConstraintName);
            return false;
        }
    }

    [RequiresDockerFact]
    public async Task Identity_RoundTripsAndDisconnectClearsPersistedClaims() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.CreateTelegram(782001, "hash");
        user.BindTelegramOidcIdentity("https://oauth.telegram.org", "oidc-subject-distinct-from-bot-id");
        user.LinkGoogleIdentity("https://accounts.google.com", "backup-subject");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        User loaded = await context.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Null(loaded.Email);
        Assert.Equal(782001, loaded.TelegramUserId);
        Assert.Equal("https://oauth.telegram.org", loaded.TelegramOidcIssuer);
        Assert.Equal("oidc-subject-distinct-from-bot-id", loaded.TelegramOidcSubject);
        loaded.UnlinkTelegram();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        User disconnected = await context.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Null(disconnected.TelegramUserId);
        Assert.Null(disconnected.TelegramOidcIssuer);
        Assert.Null(disconnected.TelegramOidcSubject);
    }

    [RequiresDockerFact]
    public async Task Identity_UniqueConstraintRejectsSameProviderSubjectOnAnotherAccount() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var first = User.CreateTelegram(782002, "hash");
        first.BindTelegramOidcIdentity("https://oauth.telegram.org", "duplicate-subject");
        context.Users.Add(first);
        await context.SaveChangesAsync();

        var second = User.CreateTelegram(782003, "hash");
        second.BindTelegramOidcIdentity("https://oauth.telegram.org", "duplicate-subject");
        context.Users.Add(second);
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        PostgresException postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("IX_Users_TelegramOidcIssuer_TelegramOidcSubject", postgres.ConstraintName);
    }
}
