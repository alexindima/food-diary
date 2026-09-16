using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
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
        FoodDiary.Modules.Users.Infrastructure.UsersModuleRegistration.AddUsersPersistence(services);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
