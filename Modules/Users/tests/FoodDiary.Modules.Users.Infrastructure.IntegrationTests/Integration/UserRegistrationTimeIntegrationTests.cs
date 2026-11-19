using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.PersistenceModel.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRegistrationTimeIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TelegramTrialRedemptionsRoundTripTheDurableTelegramIdentity() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        const long telegramId = 778899;
        context.Add(new TelegramPremiumTrialRedemption(telegramId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        TelegramPremiumTrialRedemption redeemed = await context.Set<TelegramPremiumTrialRedemption>().AsNoTracking().SingleAsync();

        Assert.Equal(telegramId, redeemed.TelegramUserId);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task RegistrationTime_ProjectsPersistedActiveUsersWithoutTrackingAndJoinsTheCallerTransaction() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("registered@example.test", "hash");
        var inactive = User.Create("inactive-registration@example.test", "hash");
        inactive.Deactivate();
        var deleted = User.Create("deleted-registration@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        DateTime registered = await context.Users.Where(user => user.Id == active.Id).Select(user => user.CreatedOnUtc).SingleAsync();
        context.ChangeTracker.Clear();
        int synchronizations = 0;
        var repository = new UserRepository(context.Users, context.UserRoleAuditEvents,
            _ => { synchronizations++; return Task.CompletedTask; });

        Assert.Equal(registered, await repository.GetRegistrationTimeAsync(active.Id));
        Assert.Null(await repository.GetRegistrationTimeAsync(inactive.Id));
        Assert.Null(await repository.GetRegistrationTimeAsync(deleted.Id));
        Assert.Null(await repository.GetRegistrationTimeAsync(UserId.New()));

        Assert.Equal(4, synchronizations);
        Assert.Empty(context.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetRegistrationTimeAsync(active.Id, cancellation.Token));
    }
}
