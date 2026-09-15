using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BodyMetricsConcurrencyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task CompetingMeasurements_ReturnConflictAndRollBackLosingScopeAsync(bool weight, bool update) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("measurement-owner@example.com", "hash");
        DateTime target = new(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        seed.Users.Add(owner);
        if (update) {
            for (int day = 1; day <= 2; day++) {
                if (weight) {
                    seed.WeightEntries.Add(WeightEntry.Create(owner.Id, target.AddDays(-day), 80));
                } else {
                    seed.WaistEntries.Add(WaistEntry.Create(owner.Id, target.AddDays(-day), 90));
                }
            }
        }
        await seed.SaveChangesAsync();
        string connection = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connection);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connection);
        await using ServiceProvider firstProvider = CreateProvider(first);
        await using ServiceProvider secondProvider = CreateProvider(second);
        // Both requests pass the duplicate lookup before either request saves.
        await StageAsync(firstProvider, owner.Id, target, target.AddDays(-1), weight, update);
        await StageAsync(secondProvider, owner.Id, target, target.AddDays(-2), weight, update);
        var firstSideEffect = User.Create("measurement-first@example.com", "hash");
        var secondSideEffect = User.Create("measurement-second@example.com", "hash");
        first.Users.Add(firstSideEffect);
        second.Users.Add(secondSideEffect);

        Exception?[] outcomes = await Task.WhenAll(
            Record.ExceptionAsync(() => firstProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()),
            Record.ExceptionAsync(() => secondProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()));

        Assert.Single(outcomes, exception => exception is null);
        DbUpdateConcurrencyException conflict = Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(outcomes.OfType<Exception>()));
        DbUpdateException providerFailure = Assert.IsType<DbUpdateException>(conflict.InnerException);
        PostgresException postgres = Assert.IsType<PostgresException>(providerFailure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(connection);
        int atTarget = weight
            ? await read.WeightEntries.CountAsync(entry => entry.UserId == owner.Id && entry.Date == target)
            : await read.WaistEntries.CountAsync(entry => entry.UserId == owner.Id && entry.Date == target);
        Assert.Equal(1, atTarget);
        Assert.Equal(outcomes[0] is null, await read.Users.AnyAsync(user => user.Id == firstSideEffect.Id));
        Assert.Equal(outcomes[1] is null, await read.Users.AnyAsync(user => user.Id == secondSideEffect.Id));
        if (update) {
            int unchanged = weight
                ? await read.WeightEntries.CountAsync(entry => entry.UserId == owner.Id && entry.Date < target)
                : await read.WaistEntries.CountAsync(entry => entry.UserId == owner.Id && entry.Date < target);
            Assert.Equal(1, unchanged);
        }
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ForeignKeyFailure_IsNotTranslatedToConcurrencyAsync(bool weight) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(seed);
        BodyMetricsDbContext metrics = provider.GetRequiredService<BodyMetricsDbContext>();
        if (weight) {
            metrics.WeightEntries.Add(WeightEntry.Create(UserId.New(), DateTime.UtcNow, 80));
        } else {
            metrics.WaistEntries.Add(WaistEntry.Create(UserId.New(), DateTime.UtcNow, 90));
        }
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    private static async Task StageAsync(ServiceProvider provider, UserId userId, DateTime target, DateTime previous, bool weight, bool update) {
        BodyMetricsDbContext metrics = provider.GetRequiredService<BodyMetricsDbContext>();
        if (weight) {
            Assert.Null(await provider.GetRequiredService<IWeightEntryWriteRepository>().GetByDateAsync(userId, target));
            if (update) {
                WeightEntry entry = await metrics.WeightEntries.SingleAsync(item => item.UserId == userId && item.Date == previous);
                entry.Update(81, target);
            } else {
                metrics.WeightEntries.Add(WeightEntry.Create(userId, target, 81));
            }
        } else {
            Assert.Null(await provider.GetRequiredService<IWaistEntryWriteRepository>().GetByDateAsync(userId, target));
            if (update) {
                WaistEntry entry = await metrics.WaistEntries.SingleAsync(item => item.UserId == userId && item.Date == previous);
                entry.Update(91, target);
            } else {
                metrics.WaistEntries.Add(WaistEntry.Create(userId, target, 91));
            }
        }
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddBodyMetricsModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
