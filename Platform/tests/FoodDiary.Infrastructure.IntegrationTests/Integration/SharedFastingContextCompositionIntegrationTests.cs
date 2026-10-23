using FoodDiary.Modules.Users.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Fasting.Application.Abstractions.Common;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedFastingContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyFiveOwnedEntities() {
        using var context = new FastingDbContext(new DbContextOptionsBuilder<FastingDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Type[] expected = [typeof(FastingPlan), typeof(FastingOccurrence), typeof(FastingCheckIn), typeof(FastingSession), typeof(FastingTelemetryEvent)];
        Assert.Equal(expected.OrderBy(type => type.Name, StringComparer.Ordinal),
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Equal("FastingSessions", context.Model.FindEntityType(typeof(FastingSession))!.GetTableName());
    }

    [RequiresDockerFact]
    public async Task SharedSavePreservesTrackedUpdateAndUserCascadeAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        FastingDbContext fasting = provider.GetRequiredService<FastingDbContext>();
        IFastingSessionRepository repository = provider.GetRequiredService<IFastingSessionRepository>();
        Assert.Same(provider.GetRequiredService<IFastingOccurrenceRepository>(), provider.GetRequiredService<IFastingOccurrenceReadRepository>());
        Assert.Same(provider.GetRequiredService<IFastingCheckInRepository>(), provider.GetRequiredService<IFastingCheckInReadRepository>());
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"fasting-context-{Guid.NewGuid():N}@example.com", "hash");
        var session = FastingSession.Create(user.Id, FastingProtocol.Fast16Eat8, 16, DateTime.UtcNow);
        central.Users.Add(user);
        await repository.AddAsync(session);
        Assert.Same(central.Database.GetDbConnection(), fasting.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<FastingSession>());
        await unitOfWork.SaveChangesAsync();
        fasting.ChangeTracker.Clear();
        Assert.Null(await repository.GetCurrentAsync(UserId.New()));
        FastingSession? tracked = await provider.GetRequiredService<IFastingSessionWriteRepository>().GetByIdAsync(session.Id, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateNotes("updated");
        await repository.UpdateAsync(tracked);
        await unitOfWork.SaveChangesAsync();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.Equal("updated", (await read.FastingSessions.SingleAsync(item => item.Id == session.Id)).Notes);
        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.FastingSessions.AnyAsync(item => item.Id == session.Id));
    }

    [RequiresDockerFact]
    public async Task SharedSave_RehydratesCheckInAndOccurrenceInTheSameOwnedContextAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        FastingDbContext fasting = provider.GetRequiredService<FastingDbContext>();
        var user = User.Create("fasting-checkin-owner@example.test", "hash");
        var started = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.Fast16Eat8, 16, 8, started);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, started, sequenceNumber: 1, targetHours: 16);
        var checkIn = FastingCheckIn.Create(occurrence.Id, user.Id, hungerLevel: 3, energyLevel: 4, moodLevel: 5,
            symptoms: null, notes: "Fixture check-in", started.AddHours(1));
        central.Users.Add(user);
        fasting.AddRange(plan, occurrence, checkIn);

        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        fasting.ChangeTracker.Clear();
        FastingOccurrence loadedOccurrence = await fasting.Set<FastingOccurrence>().SingleAsync(item => item.Id == occurrence.Id);
        FastingCheckIn loadedCheckIn = await fasting.Set<FastingCheckIn>().Include(item => item.Occurrence)
            .SingleAsync(item => item.Id == checkIn.Id);

        Assert.Multiple(
            () => Assert.Same(loadedOccurrence, loadedCheckIn.Occurrence),
            () => Assert.Equal(occurrence.Id, loadedCheckIn.OccurrenceId),
            () => Assert.Equal(user.Id, loadedCheckIn.UserId),
            () => Assert.Equal(plan.Id, loadedOccurrence.PlanId),
            () => Assert.Equal(user.Id, loadedOccurrence.UserId),
            () => Assert.Equal(new DateTime(2026, 10, 21, 13, 0, 0, DateTimeKind.Utc), loadedCheckIn.CheckedInAtUtc),
            () => Assert.Equal(DateTimeKind.Utc, loadedCheckIn.CheckedInAtUtc.Kind),
            () => Assert.Empty(central.ChangeTracker.Entries<FastingOccurrence>()),
            () => Assert.Empty(central.ChangeTracker.Entries<FastingCheckIn>()));
    }

    [RequiresDockerFact]
    public async Task InvalidOwnerRollsBackCentralUserAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var user = User.Create($"fasting-rollback-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await provider.GetRequiredService<IFastingSessionRepository>().AddAsync(
            FastingSession.Create(UserId.New(), FastingProtocol.Fast16Eat8, 16, DateTime.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddFastingModule();
        UsersModuleRegistration.AddUsersPersistence(services);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
