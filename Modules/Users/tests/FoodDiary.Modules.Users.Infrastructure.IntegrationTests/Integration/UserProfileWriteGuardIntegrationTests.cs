using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserProfileWriteGuardIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task NewAccount_CanInsertItsProfilesInTheSameSave() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using UsersDbContext owner = CreateOwner(database);
        var user = User.Create("guard-new@example.test", "hash");
        user.SetLanguage("ru");
        owner.Users.Add(user);

        await owner.SaveChangesAsync();

        Assert.Null(owner.Database.CurrentTransaction);
        User persisted = await database.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        Assert.Equal("ru", persisted.Language);
        Assert.Equal(user.SecurityVersion, persisted.SecurityVersion);
    }

    [RequiresDockerFact]
    public async Task ExistingProfile_RequiresTheCallerTransactionBeforeSaving() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("guard-no-transaction@example.test", "hash");
        user.SetLanguage("en");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        await using UsersDbContext owner = CreateOwner(database);
        User tracked = await owner.Users.SingleAsync(item => item.Id == user.Id);
        tracked.SetLanguage("ru");

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => owner.SaveChangesAsync());

        Assert.Equal("Profile writes require the coordinated unit of work transaction.", error.Message);
        Assert.Equal("en", (await database.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).Language);
    }

    [RequiresDockerTheory]
    [InlineData("version")]
    [InlineData("deleted")]
    [InlineData("missing")]
    public async Task ProfileWrite_AfterAnAccountLifecycleChange_IsRejectedWithoutPersistingPreferences(string change) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("guard-conflict@example.test", "hash");
        user.SetLanguage("en");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        await using UsersDbContext owner = CreateOwner(database);
        User tracked = await owner.Users.SingleAsync(item => item.Id == user.Id);
        tracked.SetLanguage("ru");
        if (string.Equals(change, "version", StringComparison.Ordinal)) {
            await database.Users.Where(item => item.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.SecurityVersion, item => item.SecurityVersion + 1));
        } else if (string.Equals(change, "deleted", StringComparison.Ordinal)) {
            await database.Users.Where(item => item.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.DeletedAt, DateTime.UtcNow).SetProperty(item => item.IsActive, valueExpression: false));
        } else {
            await database.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
        }

        await using (IDbContextTransaction transaction = await owner.Database.BeginTransactionAsync()) {
            DbUpdateConcurrencyException error = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => owner.SaveChangesAsync());
            Assert.Equal("The account changed before its profile could be saved.", error.Message);
            await transaction.RollbackAsync();
        }

        User? persisted = await database.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == user.Id);
        if (string.Equals(change, "missing", StringComparison.Ordinal)) {
            Assert.Null(persisted);
        } else {
            Assert.NotNull(persisted);
            Assert.Equal("en", persisted.Language);
        }
    }

    [RequiresDockerFact]
    public async Task UnchangedAccount_AllowsProfileCommitInsideTheCallerTransaction() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("guard-commit@example.test", "hash");
        user.SetLanguage("en");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        await using UsersDbContext owner = CreateOwner(database);
        User tracked = await owner.Users.SingleAsync(item => item.Id == user.Id);
        tracked.SetLanguage("ru");

        await using (IDbContextTransaction transaction = await owner.Database.BeginTransactionAsync()) {
            await owner.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        User persisted = await database.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        Assert.Equal("ru", persisted.Language);
        Assert.Equal(user.SecurityVersion, persisted.SecurityVersion);
        Assert.Equal(user.ModifiedOnUtc, persisted.ModifiedOnUtc);
    }

    private static UsersDbContext CreateOwner(FoodDiaryDbContext database) => new(new DbContextOptionsBuilder<UsersDbContext>()
        .UseNpgsql(database.Database.GetConnectionString())
        .AddInterceptors(new UserProfileWriteGuardInterceptor()).Options);
}
