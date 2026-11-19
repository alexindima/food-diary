using System.Data.Common;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Authentication;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramOperationResumeRaceTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task ConcurrentOfferRefreshIsRetriedOnceWithoutOverwritingACompetingCheckpoint(int conflicts, bool expected) {
        await using IdentityDbContext setup = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        DateTime now = DateTime.UtcNow;
        var clock = new FixedClock(now);
        var protection = new EphemeralDataProtectionProvider();
        var store = new TelegramOperationStore(setup, protection, clock);
        var user = UserId.New();
        TelegramOperationId? registered = await store.RegisterAsync(123, 10, user, 1, "photo", CancellationToken.None);
        Assert.NotNull(registered);
        TelegramOperationId id = registered.Value;
        TelegramOperationLease lease = Assert.IsType<TelegramOperationLease>(await store.AcquireAsync(123, id, CancellationToken.None));
        Assert.True(await store.CheckpointAsync(123, id, lease.LeaseId, "offered", completed: false, now.AddMinutes(10), CancellationToken.None));
        string connection = setup.Database.GetConnectionString()!;
        await using IdentityDbContext competitor = IdentityContextTestFactory.Create(connection);
        IDataProtector cipher = protection.CreateProtector("FoodDiary.Telegram.Operations.v1").CreateProtector(id.Value.ToString("N"));
        var refresh = new ConcurrentRefresh(competitor, cipher, id.Value, conflicts);
        await using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connection).AddInterceptors(refresh).Options);
        int synchronizations = 0;
        var resuming = new TelegramOperationStore(context, protection, clock, _ => { synchronizations++; return Task.CompletedTask; });

        bool resumed = await resuming.ResumeAsync(123, id, user, 1, "offered", "accepted", CancellationToken.None);

        TelegramOperation persisted = await setup.Set<TelegramOperation>().AsNoTracking().SingleAsync();
        Assert.Equal(expected, resumed);
        Assert.Equal(conflicts, refresh.Refreshes);
        Assert.Equal(1, synchronizations);
        Assert.Equal(expected ? "accepted" : "offered", cipher.Unprotect(persisted.ProtectedCheckpoint!));
        Assert.False(persisted.Completed);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ConcurrentRefresh(IdentityDbContext competitor, IDataProtector cipher, Guid operationId, int conflicts) : DbCommandInterceptor {
        public int Refreshes { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            if (Refreshes < conflicts && command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) &&
                command.CommandText.Contains("ProtectedCheckpoint", StringComparison.Ordinal)) {
                Refreshes++;
                string refreshed = cipher.Protect("offered");
                await competitor.Set<TelegramOperation>().Where(item => item.Id == operationId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ProtectedCheckpoint, refreshed), cancellationToken);
            }
            return result;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
