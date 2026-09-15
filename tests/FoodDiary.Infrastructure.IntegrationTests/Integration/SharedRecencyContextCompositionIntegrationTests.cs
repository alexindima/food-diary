using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedRecencyContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task LateTransactionRollbackAndSubsequentCommitRemainVisibleAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recents-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        RecentItemsDbContext owned = provider.GetRequiredService<RecentItemsDbContext>();
        Assert.Equal(typeof(RecentItem), Assert.Single(owned.Model.GetEntityTypes()).ClrType);
        Assert.Same(central.Database.GetDbConnection(), owned.Database.GetDbConnection());
        IRecentItemRepository repository = provider.GetRequiredService<IRecentItemRepository>();
        var productId = ProductId.New();
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await repository.RegisterUsageAsync(user.Id, [productId], []);
            Assert.Single(await repository.GetRecentProductsAsync(user.Id, 10));
            await transaction.RollbackAsync();
        }
        Assert.Empty(await repository.GetRecentProductsAsync(user.Id, 10));
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await repository.RegisterUsageAsync(user.Id, [productId], []);
            await transaction.CommitAsync();
        }
        await repository.RegisterUsageAsync(user.Id, [productId], []);
        Assert.Equal(2, Assert.Single(await repository.GetRecentProductsAsync(user.Id, 10)).UsageCount);
        Assert.Empty(central.ChangeTracker.Entries<RecentItem>());
        central.Users.Remove(user);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Empty(await repository.GetRecentProductsAsync(user.Id, 10));
    }

    [RequiresDockerFact]
    public async Task RecorderWaitsForFlushAndDiscardDoesNotWriteAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var user = User.Create($"recents-queue-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        IRecentItemUsageRecorder recorder = provider.GetRequiredService<IRecentItemUsageRecorder>();
        IPostCommitActionQueue queue = provider.GetRequiredService<IPostCommitActionQueue>();
        IRecentItemRepository repository = provider.GetRequiredService<IRecentItemRepository>();
        var productId = ProductId.New();
        await recorder.RegisterUsageAsync(user.Id, [productId], []);
        Assert.Empty(await repository.GetRecentProductsAsync(user.Id, 10));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Empty(await repository.GetRecentProductsAsync(user.Id, 10));
        await queue.FlushAsync();
        Assert.Equal(productId, Assert.Single(await repository.GetRecentProductsAsync(user.Id, 10)).ProductId);
        await recorder.RegisterUsageAsync(user.Id, [ProductId.New()], []);
        queue.Discard();
        await queue.FlushAsync();
        Assert.Single(await repository.GetRecentProductsAsync(user.Id, 10));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddApplicationRuntime();
        services.AddRecentItemsModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
