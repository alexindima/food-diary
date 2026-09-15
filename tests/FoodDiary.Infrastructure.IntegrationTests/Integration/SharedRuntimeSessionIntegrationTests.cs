using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;

using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedRuntimeSessionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComposedReadsSeeIntermediateWritesAndOuterRollbackIncludesSharedRecordsAsync(bool resolveReadContextEarly) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("runtime-session@example.com", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        SharedPersistenceDbContext runtime = provider.GetRequiredService<SharedPersistenceDbContext>();
        FoodDiaryDbContext? read = resolveReadContextEarly ? provider.GetRequiredService<FoodDiaryDbContext>() : null;
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        var failure = new InvalidOperationException("Rollback the entire attempt");

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteSerializableAsync<bool>(async token => {
            hydration.HydrationEntries.Add(HydrationEntry.Create(user.Id, DateTime.UtcNow, 250));
            runtime.EmailOutbox.Add(EmailOutboxMessage.Create(
                new EmailMessage("from@example.com", "Test", ["to@example.com"], "Atomic delivery", "Body", TextBody: null), DateTime.UtcNow));
            await unitOfWork.SaveChangesAsync(token);
            read ??= provider.GetRequiredService<FoodDiaryDbContext>();
            ICompositionReadContext queries = provider.GetRequiredService<ICompositionReadContext>();
            Assert.Same(read, queries);
            Assert.Same(runtime.Database.GetDbConnection(), read.Database.GetDbConnection());
            Assert.NotNull(read.Database.CurrentTransaction);
            Assert.Equal(250, await queries.HydrationEntries.Join(queries.Users,
                entry => entry.UserId, item => item.Id, (entry, item) => entry.AmountMl).SingleAsync(token));
            Assert.Equal(1, await queries.Users.Where(item => item.Id == user.Id)
                .Select(item => queries.HydrationEntries.Count(entry => entry.UserId == item.Id)).SingleAsync(token));
            Assert.Empty(read.ChangeTracker.Entries<HydrationEntry>());
            Assert.Single(await read.EmailOutbox.AsNoTracking().ToListAsync(token));
            throw failure;
        }));

        Assert.Same(failure, actual);
        Assert.False(unitOfWork.HasPendingChanges);
        Assert.Null(runtime.Database.CurrentTransaction);
        Assert.Null(hydration.Database.CurrentTransaction);
        Assert.Null(read!.Database.CurrentTransaction);
        Assert.Empty(await database.HydrationEntries.AsNoTracking().ToListAsync());
        Assert.Empty(await database.EmailOutbox.AsNoTracking().ToListAsync());
        Assert.False(database.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task RuntimeAndOwnerRecordsCommitTogetherWithoutResolvingFullContextAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("runtime-commit@example.com", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        SharedPersistenceDbContext runtime = provider.GetRequiredService<SharedPersistenceDbContext>();
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        hydration.HydrationEntries.Add(HydrationEntry.Create(user.Id, DateTime.UtcNow, 300));
        runtime.EmailOutbox.Add(EmailOutboxMessage.Create(
            new EmailMessage("from@example.com", "Test", ["to@example.com"], "Committed delivery", "Body", TextBody: null), DateTime.UtcNow));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.DoesNotContain(runtime.ModuleContexts, context => context is FoodDiaryDbContext);
        Assert.Single(await database.HydrationEntries.AsNoTracking().ToListAsync());
        Assert.Single(await database.EmailOutbox.AsNoTracking().ToListAsync());
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "true",
        }).Build()).AddOutboxProcessing(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "true",
        }).Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddHydrationModule();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        return services.BuildServiceProvider();
    }

    [RequiresDockerFact]
    public async Task OwnerSaveFailureRollsBackEarlierEmailInsertAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        SharedPersistenceDbContext runtime = provider.GetRequiredService<SharedPersistenceDbContext>();
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        hydration.HydrationEntries.Add(HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 300));
        runtime.EmailOutbox.Add(EmailOutboxMessage.Create(
            new EmailMessage("from@example.com", "Test", ["to@example.com"], "Must roll back", "Body", TextBody: null), DateTime.UtcNow));
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
        Assert.True(unitOfWork.HasPendingChanges);
        Assert.Null(hydration.Database.CurrentTransaction);
        Assert.Empty(await database.EmailOutbox.AsNoTracking().ToListAsync());
        Assert.Empty(await database.HydrationEntries.AsNoTracking().ToListAsync());
    }

    [RequiresDockerFact]
    public async Task CallerManagedTransactionRetainsSaveTimeDetachPolicyAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("runtime-caller@example.com", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        SharedPersistenceDbContext runtime = provider.GetRequiredService<SharedPersistenceDbContext>();
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        await runtime.Database.CreateExecutionStrategy().ExecuteAsync(async () => {
            await using IDbContextTransaction transaction = await runtime.Database.BeginTransactionAsync();
            await hydration.Database.UseTransactionAsync(transaction.GetDbTransaction());
            hydration.HydrationEntries.Add(HydrationEntry.Create(user.Id, DateTime.UtcNow, 300));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            Assert.Null(hydration.Database.CurrentTransaction);
            await transaction.CommitAsync();
        });
        Assert.Single(await hydration.HydrationEntries.AsNoTracking().ToListAsync());
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
