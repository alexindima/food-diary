using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class PersistenceParticipationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task AccountPrincipals_SaveBeforeDependantsRegardlessOfEnlistmentOrder() {
        await using FoodDiaryDbContext root = await databaseFixture.CreateDbContextAsync();
        await using MealsDbContext meals = root.CreateModuleContext<MealsDbContext>(static options => new MealsDbContext(options));
        await using UsersDbContext users = root.CreateModuleContext<UsersDbContext>(static options => new UsersDbContext(options), PersistenceSaveOrder.AccountPrincipals);
        var user = User.Create("ordered-participants@example.com", "hash");
        users.Users.Add(user);
        meals.Meals.Add(Meal.Create(user.Id, DateTime.UtcNow));
        var unitOfWork = new EfUnitOfWork(root, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance);
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(1, await root.Users.AsNoTracking().CountAsync());
        Assert.Equal(1, await root.Meals.AsNoTracking().CountAsync());
    }

    [RequiresDockerFact]
    public async Task EnlistmentDuringSaving_IsRejectedAndRollsBackEveryParticipant() {
        await using FoodDiaryDbContext root = await databaseFixture.CreateDbContextAsync();
        await using UsersDbContext users = root.CreateModuleContext<UsersDbContext>(options => new UsersDbContext(
            new DbContextOptionsBuilder<UsersDbContext>(options).AddInterceptors(new EnlistDuringSave(root)).Options));
        users.Users.Add(User.Create("late-participant@example.com", "hash"));
        var unitOfWork = new EfUnitOfWork(root, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.SaveChangesAsync());
        Assert.Empty(await root.Users.AsNoTracking().ToArrayAsync());
        Assert.Empty(await root.Meals.AsNoTracking().ToArrayAsync());
    }

    [ExcludeFromCodeCoverage]
    private sealed class EnlistDuringSave(FoodDiaryDbContext root) : SaveChangesInterceptor {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            await using MealsDbContext ignored = root.CreateModuleContext<MealsDbContext>(static options => new MealsDbContext(options));
            return result;
        }
    }
}
