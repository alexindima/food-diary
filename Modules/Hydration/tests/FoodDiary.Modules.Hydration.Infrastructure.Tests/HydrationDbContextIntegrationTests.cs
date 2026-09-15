using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationDbContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyOwnedEntitiesWithExistingTableNames() {
        using var context = new HydrationDbContext(new DbContextOptionsBuilder<HydrationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);

        Assert.Equal([typeof(HydrationEntry), typeof(HydrationOperationReceipt)],
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Equal("HydrationEntries", context.Model.FindEntityType(typeof(HydrationEntry))!.GetTableName());
        Assert.Equal("HydrationOperationReceipts", context.Model.FindEntityType(typeof(HydrationOperationReceipt))!.GetTableName());
        Assert.True(context.Model.FindEntityType(typeof(HydrationEntry))!.FindProperty("xmin")!.IsConcurrencyToken);
    }

    [RequiresDockerFact]
    public async Task UnitOfWorkCommitsBothContextsAndPreservesCascadeAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"context-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        context.Users.Add(user);
        hydration.AddRange(entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry));

        Assert.Same(context.Database.GetDbConnection(), hydration.Database.GetDbConnection());
        Assert.Empty(context.ChangeTracker.Entries<HydrationEntry>());
        Assert.True(unitOfWork.HasPendingChanges);
        await unitOfWork.SaveChangesAsync();

        Assert.False(unitOfWork.HasPendingChanges);
        Assert.Null(hydration.Database.CurrentTransaction);
        Assert.True(await context.HydrationEntries.AsNoTracking().AnyAsync(item => item.Id == entry.Id));
        await context.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
        Assert.False(await hydration.HydrationEntries.AsNoTracking().AnyAsync(item => item.UserId == user.Id));
        Assert.False(await hydration.HydrationOperationReceipts.AsNoTracking().AnyAsync(item => item.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task DuplicateReceiptRollsBackCentralAndModuleWritesAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"owner-{Guid.NewGuid():N}@example.com", "hash");
        var first = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var operationId = Guid.NewGuid();
        context.AddRange(user, first, HydrationOperationReceipt.Create(operationId, first));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context);
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        var marker = User.Create($"rollback-{Guid.NewGuid():N}@example.com", "hash");
        var duplicate = HydrationEntry.Create(user.Id, DateTime.UtcNow, 500);
        context.Users.Add(marker);
        hydration.AddRange(duplicate, HydrationOperationReceipt.Create(operationId, duplicate));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());

        Assert.False(await context.Users.AsNoTracking().AnyAsync(item => item.Id == marker.Id));
        Assert.False(await hydration.HydrationEntries.AsNoTracking().AnyAsync(item => item.Id == duplicate.Id));
        Assert.Null(hydration.Database.CurrentTransaction);
    }

    [RequiresDockerFact]
    public async Task CallerTransactionCanRollBackSuccessfulUnitOfWorkAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        HydrationDbContext hydration = provider.GetRequiredService<HydrationDbContext>();
        var user = User.Create($"ambient-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        context.Users.Add(user);
        hydration.AddRange(entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry));
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await transaction.RollbackAsync();

        Assert.False(await context.Users.AsNoTracking().AnyAsync(item => item.Id == user.Id));
        Assert.False(await hydration.HydrationEntries.AsNoTracking().AnyAsync(item => item.Id == entry.Id));
    }

    [RequiresDockerFact]
    public async Task TransientModuleFailureRetriesEntireAtomicSaveAsync() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        await context.Database.MigrateAsync();
        var interceptor = new FailFirstModuleSave();
        await using HydrationDbContext hydration = context.CreateModuleContext<HydrationDbContext>(options => new HydrationDbContext(
            new DbContextOptionsBuilder<HydrationDbContext>(options).AddInterceptors(interceptor).Options));
        await using ServiceProvider provider = CreateProvider(context);
        var user = User.Create($"retry-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        context.Users.Add(user);
        hydration.AddRange(entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry));

        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        Assert.Equal(2, interceptor.Attempts);
        Assert.Equal(1, await context.Users.CountAsync(item => item.Id == user.Id));
        Assert.Equal(1, await hydration.HydrationEntries.CountAsync(item => item.Id == entry.Id));
        Assert.Equal(1, await hydration.HydrationOperationReceipts.CountAsync(item => item.UserId == user.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddHydrationModule();
        return services.BuildServiceProvider();
    }

    [RequiresDockerFact]
    public async Task CancellationAfterCentralSaveRollsBackBothContextsAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        using var cancellation = new CancellationTokenSource();
        await using HydrationDbContext hydration = context.CreateModuleContext<HydrationDbContext>(options => new HydrationDbContext(
            new DbContextOptionsBuilder<HydrationDbContext>(options).AddInterceptors(new CancelModuleSave(cancellation)).Options));
        await using ServiceProvider provider = CreateProvider(context);
        var user = User.Create($"cancel-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        context.Users.Add(user);
        hydration.AddRange(entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellation.Token));

        Assert.False(await context.Users.AsNoTracking().AnyAsync(item => item.Id == user.Id));
        Assert.False(await hydration.HydrationEntries.AsNoTracking().AnyAsync(item => item.Id == entry.Id));
        Assert.Null(hydration.Database.CurrentTransaction);
    }

    private sealed class CancelModuleSave(CancellationTokenSource cancellation) : SaveChangesInterceptor {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            await cancellation.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }

    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailFirstModuleSave : SaveChangesInterceptor {
        public int Attempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            if (++Attempts == 1) {
                throw new NpgsqlException("Injected transient failure before module write.", new TimeoutException());
            }
            return ValueTask.FromResult(result);
        }
    }
}
