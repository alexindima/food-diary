using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.Infrastructure;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class CyclesContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyTheEightOwnedEntitiesWithExistingTables() {
        using var context = new CyclesDbContext(new DbContextOptionsBuilder<CyclesDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Type[] expected = [typeof(CycleProfile), typeof(BleedingEntry), typeof(CycleSymptomEntry), typeof(CycleFactor),
            typeof(FertilitySignal), typeof(MenstrualEpisode), typeof(CycleConsent), typeof(CyclePredictionRevision)];
        Assert.Equal(expected.OrderBy(type => type.Name, StringComparer.Ordinal),
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        string[] tables = ["CycleProfiles", "CycleBleedingEntries", "CycleSymptomEntries", "CycleFactors",
            "FertilitySignals", "CycleMenstrualEpisodes", "CycleConsents", "CyclePredictionRevisions"];
        Assert.Equal(tables.Order(StringComparer.Ordinal),
            context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Order(StringComparer.Ordinal));
    }

    [RequiresDockerFact]
    public async Task SharedSavePreservesNestedTrackingReadProjectionAndUserCascadeAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        CyclesDbContext cycles = provider.GetRequiredService<CyclesDbContext>();
        ICycleWriteRepository writes = provider.GetRequiredService<ICycleWriteRepository>();
        ICycleReadModelRepository reads = provider.GetRequiredService<ICycleReadModelRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"cycle-context-{Guid.NewGuid():N}@example.com", "hash");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, today.AddDays(-28));
        MenstrualEpisode episode = profile.ConfirmPeriodStart(today.AddDays(-4));
        central.Users.Add(user);
        await writes.AddAsync(profile);
        Assert.Same(central.Database.GetDbConnection(), cycles.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<CycleProfile>());
        await unitOfWork.SaveChangesAsync();
        Assert.False(unitOfWork.HasPendingChanges);

        cycles.ChangeTracker.Clear();
        Assert.Null(await writes.GetByIdAsync(profile.Id, UserId.New(), includeDetails: true, asTracking: true));
        Assert.Null(await reads.GetCurrentReadModelAsync(UserId.New()));
        CycleProfile? tracked = await writes.GetByIdAsync(profile.Id, user.Id, includeDetails: true, asTracking: true);
        Assert.NotNull(tracked);
        Assert.Single(tracked.Consents);
        Assert.Single(tracked.MenstrualEpisodes);
        tracked.UpdateMenstrualEpisode(episode.Id, episode.StartDate, today.AddDays(-1));
        await writes.UpdateAsync(tracked);
        await unitOfWork.SaveChangesAsync();

        CycleProfileReadModel? snapshot = await reads.GetCurrentReadModelAsync(user.Id);
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.MenstrualEpisodes);
        Assert.Equal(today.AddDays(-1), Assert.Single(snapshot.MenstrualEpisodes).EndDate);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.Equal(today.AddDays(-1), (await read.CycleMenstrualEpisodes.SingleAsync(item => item.Id == episode.Id)).EndDate);
        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.CycleProfiles.AnyAsync(item => item.Id == profile.Id));
        Assert.False(await read.CycleConsents.AnyAsync(item => item.CycleProfileId == profile.Id));
        Assert.False(await read.CycleMenstrualEpisodes.AnyAsync(item => item.CycleProfileId == profile.Id));
    }

    [RequiresDockerFact]
    public async Task DuplicateProfileRollsBackCentralUserAndNestedRowsAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        CyclesDbContext cycles = provider.GetRequiredService<CyclesDbContext>();
        var user = User.Create($"cycle-rollback-{Guid.NewGuid():N}@example.com", "hash");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, today);
        profile.ConfirmPeriodStart(today);
        central.Users.Add(user);
        cycles.CycleProfiles.AddRange(profile, CycleProfile.Create(user.Id, today));

        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        Assert.Null(cycles.Database.CurrentTransaction);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.CycleProfiles.AnyAsync(item => item.UserId == user.Id));
        Assert.False(await read.CycleConsents.AnyAsync(item => item.CycleProfileId == profile.Id));
        Assert.False(await read.CycleMenstrualEpisodes.AnyAsync(item => item.CycleProfileId == profile.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddCyclesModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
