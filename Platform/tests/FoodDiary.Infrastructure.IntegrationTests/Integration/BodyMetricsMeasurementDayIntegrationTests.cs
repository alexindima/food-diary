using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Domain.Primitives;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BodyMetricsMeasurementDayIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TypedDay_RoundTripsThroughOwnerContextAndExistingDateColumnAsync(bool weight) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"measurement-day-{Guid.NewGuid():N}@example.com", "hash");
        var otherOwner = User.Create($"measurement-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(owner, otherOwner);
        await using ServiceProvider provider = CreateProvider(context);
        BodyMetricsDbContext metrics = provider.GetRequiredService<BodyMetricsDbContext>();
        IUnitOfWork unit = provider.GetRequiredService<IUnitOfWork>();
        var leapDay = new MeasurementDay(new DateOnly(2024, 2, 29));
        var nextDay = new MeasurementDay(new DateOnly(2024, 3, 1));
        Assert.False(context.Database.HasPendingModelChanges());
        IEntityType? entity = metrics.Model.FindEntityType(weight ? typeof(WeightEntry) : typeof(WaistEntry));
        Assert.NotNull(entity);
        IProperty? date = entity.FindProperty(nameof(WeightEntry.Date));
        Assert.NotNull(date);
        Assert.Multiple(() => {
            Assert.Equal(typeof(DateTime), date.ClrType);
            Assert.Equal("date", date.GetColumnType());
            Assert.Null(metrics.Model.FindEntityType(typeof(MeasurementDay)));
            Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(WeightEntry.UserId), nameof(WeightEntry.Date)], StringComparer.Ordinal));
        });

        if (weight) {
            IWeightEntryWriteRepository repository = provider.GetRequiredService<IWeightEntryWriteRepository>();
            var entry = WeightEntry.CreateForDay(owner.Id, leapDay, 72.125);
            await repository.AddAsync(entry);
            await unit.SaveChangesAsync();
            metrics.ChangeTracker.Clear();
            WeightEntry? loaded = await repository.GetByDateAsync(owner.Id, leapDay.ToUtcDateTime());
            Assert.NotNull(loaded);
            Assert.Equal(leapDay.Value, DateOnly.FromDateTime(loaded.Date));
            Assert.Equal(72.125, loaded.WeightKg);
            loaded.UpdateDetails(weight: 73.125, day: nextDay);
            await repository.UpdateAsync(loaded);
            await unit.SaveChangesAsync();
            metrics.ChangeTracker.Clear();
            WeightEntry? reloaded = await repository.GetByDateAsync(owner.Id, nextDay.ToUtcDateTime());
            Assert.NotNull(reloaded);
            Assert.Equal(entry.Id, reloaded.Id);
            Assert.Equal(nextDay, MeasurementDay.FromDateTimeEncoding(reloaded.Date));
            Assert.Equal(73.125, reloaded.WeightKg);
            Assert.Null(await repository.GetByDateAsync(owner.Id, leapDay.ToUtcDateTime()));
            Assert.Null(await repository.GetByDateAsync(otherOwner.Id, nextDay.ToUtcDateTime()));
        } else {
            IWaistEntryWriteRepository repository = provider.GetRequiredService<IWaistEntryWriteRepository>();
            var entry = WaistEntry.CreateForDay(owner.Id, leapDay, 85.125);
            await repository.AddAsync(entry);
            await unit.SaveChangesAsync();
            metrics.ChangeTracker.Clear();
            WaistEntry? loaded = await repository.GetByDateAsync(owner.Id, leapDay.ToUtcDateTime());
            Assert.NotNull(loaded);
            Assert.Equal(leapDay.Value, DateOnly.FromDateTime(loaded.Date));
            Assert.Equal(85.125, loaded.CircumferenceCm);
            loaded.UpdateDetails(circumference: 86.125, day: nextDay);
            await repository.UpdateAsync(loaded);
            await unit.SaveChangesAsync();
            metrics.ChangeTracker.Clear();
            WaistEntry? reloaded = await repository.GetByDateAsync(owner.Id, nextDay.ToUtcDateTime());
            Assert.NotNull(reloaded);
            Assert.Equal(entry.Id, reloaded.Id);
            Assert.Equal(nextDay, MeasurementDay.FromDateTimeEncoding(reloaded.Date));
            Assert.Equal(86.125, reloaded.CircumferenceCm);
            Assert.Null(await repository.GetByDateAsync(owner.Id, leapDay.ToUtcDateTime()));
            Assert.Null(await repository.GetByDateAsync(otherOwner.Id, nextDay.ToUtcDateTime()));
        }
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build())
            .AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
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
