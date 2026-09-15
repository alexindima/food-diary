using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Exercises.Domain.Enums;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.Exercises.Infrastructure;
using FoodDiary.Modules.Exercises.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ModuleContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ExercisesModelContainsOnlyItsEntryAndExistingTable() {
        using var context = new ExercisesDbContext(new DbContextOptionsBuilder<ExercisesDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.Equal(typeof(ExerciseEntry), Assert.Single(context.Model.GetEntityTypes()).ClrType);
        Assert.Equal("ExerciseEntries", context.Model.FindEntityType(typeof(ExerciseEntry))!.GetTableName());
    }

    [Fact]
    public void BodyMetricsModelContainsOnlyMeasurements() {
        using var context = new BodyMetricsDbContext(new DbContextOptionsBuilder<BodyMetricsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.Equal([typeof(WaistEntry), typeof(WeightEntry)],
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Equal("WeightEntries", context.Model.FindEntityType(typeof(WeightEntry))!.GetTableName());
        Assert.Equal("WaistEntries", context.Model.FindEntityType(typeof(WaistEntry))!.GetTableName());
    }

    [RequiresDockerFact]
    public async Task SharedUnitOfWorkCommitsFourContextsAndPreservesUserIsolationAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        BodyMetricsDbContext metrics = provider.GetRequiredService<BodyMetricsDbContext>();
        IWeightEntryWriteRepository weights = provider.GetRequiredService<IWeightEntryWriteRepository>();
        IWeightEntryReadModelRepository weightsRead = provider.GetRequiredService<IWeightEntryReadModelRepository>();
        IWaistEntryWriteRepository waists = provider.GetRequiredService<IWaistEntryWriteRepository>();
        IWaistEntryReadModelRepository waistsRead = provider.GetRequiredService<IWaistEntryReadModelRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"contexts-{Guid.NewGuid():N}@example.com", "hash");
        ExercisesDbContext exercises = provider.GetRequiredService<ExercisesDbContext>();
        IExerciseEntryWriteRepository exerciseWrites = provider.GetRequiredService<IExerciseEntryWriteRepository>();
        IExerciseEntryReadModelRepository exerciseReads = provider.GetRequiredService<IExerciseEntryReadModelRepository>();
        central.Users.Add(user);
        var water = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        hydration.HydrationEntries.Add(water);
        WeightEntry weight = await weights.AddAsync(WeightEntry.Create(user.Id, DateTime.UtcNow, 80));
        WaistEntry waist = await waists.AddAsync(WaistEntry.Create(user.Id, DateTime.UtcNow, 90));
        ExerciseEntry exercise = await exerciseWrites.AddAsync(ExerciseEntry.Create(user.Id, DateTime.UtcNow, ExerciseType.Cardio, 30, 200));

        Assert.Same(central.Database.GetDbConnection(), metrics.Database.GetDbConnection());
        Assert.Same(central.Database.GetDbConnection(), hydration.Database.GetDbConnection());
        Assert.Same(central.Database.GetDbConnection(), exercises.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<WeightEntry>());
        Assert.True(unitOfWork.HasPendingChanges);
        await unitOfWork.SaveChangesAsync();
        Assert.False(unitOfWork.HasPendingChanges);

        Assert.Null(await weights.GetByIdAsync(weight.Id, UserId.New(), asTracking: true));
        Assert.Null(await waists.GetByIdAsync(waist.Id, UserId.New(), asTracking: true));
        Assert.Empty(await weightsRead.GetEntryReadModelsAsync(UserId.New(), dateFrom: null, dateTo: null, limit: null, descending: true));
        Assert.Empty(await waistsRead.GetEntryReadModelsAsync(UserId.New(), dateFrom: null, dateTo: null, limit: null, descending: true));
        Assert.Null(await exerciseWrites.GetByIdAsync(exercise.Id, UserId.New(), asTracking: true));
        Assert.Empty(await exerciseReads.GetByDateRangeReadModelsAsync(UserId.New(), DateTime.UtcNow.Date, DateTime.UtcNow.Date));

        exercises.ChangeTracker.Clear();
        ExerciseEntry? trackedExercise = await exerciseWrites.GetByIdAsync(exercise.Id, user.Id, asTracking: true);
        Assert.NotNull(trackedExercise);
        trackedExercise.Update(caloriesBurned: 220);
        await exerciseWrites.UpdateAsync(trackedExercise);

        weight.Update(weight: 79);
        await weights.UpdateAsync(weight);
        await waists.DeleteAsync(waist);
        await unitOfWork.SaveChangesAsync();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.Equal(79, (await read.WeightEntries.SingleAsync(entry => entry.Id == weight.Id)).WeightKg);
        Assert.False(await read.WaistEntries.AnyAsync(entry => entry.Id == waist.Id));
        Assert.True(await read.HydrationEntries.AnyAsync(entry => entry.Id == water.Id));
        Assert.Equal(220, (await read.ExerciseEntries.SingleAsync(entry => entry.Id == exercise.Id)).CaloriesBurned);

        await exerciseWrites.DeleteAsync(trackedExercise);
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.ExerciseEntries.AnyAsync(entry => entry.Id == exercise.Id));
        ExerciseEntry cascadeExercise = await exerciseWrites.AddAsync(ExerciseEntry.Create(user.Id, DateTime.UtcNow, ExerciseType.Cardio, 20, 100));
        await unitOfWork.SaveChangesAsync();

        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.WeightEntries.AnyAsync(entry => entry.Id == weight.Id));
        Assert.False(await read.HydrationEntries.AnyAsync(entry => entry.Id == water.Id));
        Assert.False(await read.ExerciseEntries.AnyAsync(entry => entry.Id == cascadeExercise.Id));
    }

    [RequiresDockerFact]
    public async Task LaterModuleFailureRollsBackEarlierModuleAndCentralWritesAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        ExercisesDbContext exercises = provider.GetRequiredService<ExercisesDbContext>();
        BodyMetricsDbContext metrics = provider.GetRequiredService<BodyMetricsDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"rollback-contexts-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        var water = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        hydration.HydrationEntries.Add(water);
        var exercise = ExerciseEntry.Create(user.Id, DateTime.UtcNow, ExerciseType.Cardio, 30, 200);
        exercises.ExerciseEntries.Add(exercise);
        DateTime today = DateTime.UtcNow.Date;
        metrics.WeightEntries.AddRange(WeightEntry.Create(user.Id, today, 80), WeightEntry.Create(user.Id, today, 81));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => unitOfWork.SaveChangesAsync());
        Assert.True(unitOfWork.HasPendingChanges);
        Assert.Null(hydration.Database.CurrentTransaction);
        Assert.Null(metrics.Database.CurrentTransaction);
        Assert.Null(exercises.Database.CurrentTransaction);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(entry => entry.Id == user.Id));
        Assert.False(await read.WeightEntries.AnyAsync(entry => entry.UserId == user.Id));
        Assert.False(await read.HydrationEntries.AnyAsync(entry => entry.Id == water.Id));
        Assert.False(await read.ExerciseEntries.AnyAsync(entry => entry.Id == exercise.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddHydrationModule();
        services.AddBodyMetricsModule();
        services.AddExercisesModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
