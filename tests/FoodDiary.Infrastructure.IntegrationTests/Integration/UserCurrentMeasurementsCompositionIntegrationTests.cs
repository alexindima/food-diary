using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserCurrentMeasurementsCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CurrentMeasurementsSelectLatestOwnedDateWithoutTrackingAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("measurement-owner@example.com", "hash");
        var other = User.Create("measurement-other@example.com", "hash");
        DateTime date = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        context.Users.AddRange(owner, other);
        context.WeightEntries.AddRange(
            WeightEntry.Create(owner.Id, date, 70),
            WeightEntry.Create(owner.Id, date.AddDays(-1), 90),
            WeightEntry.Create(other.Id, date.AddDays(1), 110));
        context.WaistEntries.AddRange(
            WaistEntry.Create(owner.Id, date, 80),
            WaistEntry.Create(owner.Id, date.AddDays(-1), 100),
            WaistEntry.Create(other.Id, date.AddDays(1), 120));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context);

        Assert.Equal(70d, await provider.GetRequiredService<IUserCurrentWeightProvider>().GetCurrentWeightAsync(owner.Id));
        Assert.Equal(80d, await provider.GetRequiredService<IUserCurrentWaistProvider>().GetCurrentWaistAsync(owner.Id));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task CurrentMeasurementsReturnNullAndHonorCancellationAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IUserCurrentWeightProvider weight = provider.GetRequiredService<IUserCurrentWeightProvider>();
        IUserCurrentWaistProvider waist = provider.GetRequiredService<IUserCurrentWaistProvider>();
        var missingUser = UserId.New();
        Assert.Null(await weight.GetCurrentWeightAsync(missingUser));
        Assert.Null(await waist.GetCurrentWaistAsync(missingUser));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => weight.GetCurrentWeightAsync(missingUser, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waist.GetCurrentWaistAsync(missingUser, cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(context);
        services.AddUsersPersistence();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }
}
