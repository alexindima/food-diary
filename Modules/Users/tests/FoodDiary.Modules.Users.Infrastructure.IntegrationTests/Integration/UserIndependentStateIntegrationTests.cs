using NSubstitute;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Persistence.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserIndependentStateIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task PreferencesNutritionAndAuthenticationActivity_HaveIndependentConcurrencyVersions() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("independent-state@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope preferences = provider.CreateAsyncScope();
        await using AsyncServiceScope nutrition = provider.CreateAsyncScope();
        await using AsyncServiceScope authentication = provider.CreateAsyncScope();
        User preferencesUser = await GetUserAsync(preferences);
        User nutritionUser = await GetUserAsync(nutrition);
        User authenticationUser = await GetUserAsync(authentication);
        preferencesUser.UpdatePreferences(new UserPreferenceUpdate(ReminderDelays: default, Theme: "dark"));
        nutritionUser.UpdateGoals(dailyCalorieTarget: 2100);
        authenticationUser.RecordAuthenticationActivity(DateTime.UtcNow);
        await authentication.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await preferences.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await nutrition.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        seed.ChangeTracker.Clear();
        User persisted = await seed.Users.SingleAsync();
        Assert.Equal("dark", persisted.Theme);
        Assert.Equal(2100, persisted.DailyCalorieTarget);
        Assert.NotNull(persisted.LastLoginAtUtc);
        Assert.NotNull(persisted.Preferences.ModifiedOnUtc);
        Assert.NotNull(persisted.NutritionProfile.ModifiedOnUtc);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingWritesToTheSameState_StillFail(bool nutritionState) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        seed.Users.Add(User.Create("same-state@example.com", "hash"));
        await seed.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope first = provider.CreateAsyncScope();
        await using AsyncServiceScope second = provider.CreateAsyncScope();
        User firstUser = await GetUserAsync(first);
        User secondUser = await GetUserAsync(second);
        if (nutritionState) {
            firstUser.UpdateGoals(dailyCalorieTarget: 2000);
            secondUser.UpdateGoals(dailyCalorieTarget: 2200);
        } else {
            firstUser.UpdatePreferences(new UserPreferenceUpdate(ReminderDelays: default, Theme: "dark"));
            secondUser.UpdatePreferences(new UserPreferenceUpdate(ReminderDelays: default, Theme: "leaf"));
        }
        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
    }

    [RequiresDockerTheory]
    [InlineData("deleted")]
    [InlineData("inactive")]
    [InlineData("credentials")]
    public async Task AccountRevocation_PreventsPreviouslyLoadedProfileWrites(string revocation) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("revoked-state@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope stale = provider.CreateAsyncScope();
        User staleUser = await GetUserAsync(stale);
        staleUser.UpdatePreferences(new UserPreferenceUpdate(ReminderDelays: default, Theme: "dark"));
        switch (revocation) {
            case "deleted": user.MarkDeleted(DateTime.UtcNow); break;
            case "inactive": user.Deactivate(); break;
            default: user.UpdatePassword("changed-hash"); break;
        }
        await seed.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        seed.ChangeTracker.Clear();
        Assert.Equal("ocean", await seed.Users.Select(account => account.Preferences.Theme).SingleAsync());
    }

    private static Task<User> GetUserAsync(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<UsersDbContext>().Users.SingleAsync();

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddPersistenceRuntime(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build());
        services.AddUsersPersistence();
        services.Replace(ServiceDescriptor.Singleton(Substitute.For<IDomainEventPublisher>()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
