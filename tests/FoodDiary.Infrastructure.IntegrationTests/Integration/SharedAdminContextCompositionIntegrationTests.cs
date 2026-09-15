using FoodDiary.Modules.Admin.Infrastructure;
using FoodDiary.Modules.Admin.Infrastructure.Persistence;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Admin.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Admin.Infrastructure.Integrations;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Admin.PersistenceModel;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedAdminContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSaveAndPurgePreserveSessionOwnershipAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        AdminDbContext owned = provider.GetRequiredService<AdminDbContext>();
        Assert.Equal(2, owned.Model.GetEntityTypes().Count());
        Assert.NotNull(owned.Model.FindEntityType(typeof(AdminImpersonationSession)));
        Assert.NotNull(owned.Model.FindEntityType(typeof(BugAcknowledgementReceipt)));
        Assert.Same(central.Database.GetDbConnection(), owned.Database.GetDbConnection());
        var actor = User.Create($"admin-context-{Guid.NewGuid():N}@example.com", "hash");
        var target = User.Create($"admin-target-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.AddRange(actor, target);
        IAdminImpersonationSessionWriteRepository repository = provider.GetRequiredService<IAdminImpersonationSessionWriteRepository>();
        await repository.AddAsync(AdminImpersonationSession.Start(actor.Id, target.Id, "Investigating support ticket",
            actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
        Assert.Empty(central.ChangeTracker.Entries<AdminImpersonationSession>());
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(1, (await provider.GetRequiredService<IAdminImpersonationSessionQuery>().GetPagedAsync(1, 10, search: null)).TotalItems);
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            foreach (IUserDataPurgeParticipant participant in provider.GetServices<IUserDataPurgeParticipant>()) {
                await participant.PurgeAsync(target.Id, reassignTarget: null, CancellationToken.None);
            }
            central.Users.Remove(target);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await transaction.CommitAsync();
        }
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.AdminImpersonationSessions.AnyAsync());
        Assert.False(await read.Users.AnyAsync(user => user.Id == target.Id));
    }

    [RequiresDockerFact]
    public async Task InvalidSessionRollsBackReceiptAndCentralUserAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var actor = User.Create($"admin-rollback-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(actor);
        await provider.GetRequiredService<IAdminImpersonationSessionWriteRepository>().AddAsync(
            AdminImpersonationSession.Start(actor.Id, UserId.New(), "Investigating support ticket",
                actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
        var inboxId = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IBugAcknowledgementReceipts>()
            .RecordAsync(inboxId, CancellationToken.None));
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(user => user.Id == actor.Id));
        Assert.False(await read.Set<BugAcknowledgementReceipt>().AnyAsync(item => item.InboxId == inboxId));
    }

    [RequiresDockerFact]
    public async Task ReceiptSavesImmediatelyAndDuplicateLeavesContextUsableAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IBugAcknowledgementReceipts receipts = provider.GetRequiredService<IBugAcknowledgementReceipts>();
        var inboxId = Guid.NewGuid();
        await receipts.RecordAsync(inboxId, CancellationToken.None);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.True(await read.Set<BugAcknowledgementReceipt>().AnyAsync(item => item.InboxId == inboxId));
        provider.GetRequiredService<AdminDbContext>().ChangeTracker.Clear();
        await receipts.RecordAsync(inboxId, CancellationToken.None);
        var nextId = Guid.NewGuid();
        await receipts.RecordAsync(nextId, CancellationToken.None);
        Assert.True(await receipts.ContainsAsync(nextId, CancellationToken.None));
        Assert.Equal(1, await read.Set<BugAcknowledgementReceipt>().CountAsync(item => item.InboxId == inboxId));
        Assert.Empty(central.ChangeTracker.Entries<BugAcknowledgementReceipt>());
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["MailInboxClient:BaseUrl"] = "http://localhost:5098",
        }).Build();
        services.AddInfrastructure(configuration);
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddAdminPersistence();
        services.AddReadModelComposition();
        services.AddAdminMailInboxIntegration(configuration);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
