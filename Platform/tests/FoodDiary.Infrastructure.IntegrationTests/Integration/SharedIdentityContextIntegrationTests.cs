using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Identity.Infrastructure;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Domain.Entities.Content;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedIdentityContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSavePersistsOwnerRecordsAndTemplateRevisionsAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        (User user, UserRefreshTokenSession session) = await SeedAsync(provider);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        IdentityDbContext owned = provider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(7, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<UserRefreshTokenSession>());
        Assert.Empty(shared.ChangeTracker.Entries<UserLoginEvent>());
        Assert.Empty(shared.ChangeTracker.Entries<EmailTemplate>());
        Assert.Equal(user.Id, (await database.UserRefreshTokenSessions.SingleAsync()).UserId);
        Assert.NotNull(await provider.GetRequiredService<IRefreshTokenSessionReadRepository>().GetByIdAsync(session.Id));
        IEmailTemplateRepository templates = provider.GetRequiredService<IEmailTemplateRepository>();
        await templates.UpsertAsync("context-test", "en", "Changed", "<p>changed</p>", "changed", isActive: true);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Single(await templates.GetRevisionsAsync("context-test", "en", CancellationToken.None));
        Assert.Contains(await templates.GetAllAsync(), template => string.Equals(template.Key, "context-test", StringComparison.Ordinal));
        Assert.Contains(await templates.GetAllReadModelsAsync(CancellationToken.None), template => string.Equals(template.Key, "context-test", StringComparison.Ordinal));
        Assert.NotNull(await ((IEmailTemplateReadRepository)templates).GetByKeyAsync("context-test", "en"));
        Assert.Single((await provider.GetRequiredService<IUserLoginEventReadRepository>().GetPagedAsync(1, 10, user.Id.Value, search: null)).Items);
        Assert.False(database.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task BulkAndTelegramOperationsJoinTransactionAfterIntermediateSaveAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        (User user, UserRefreshTokenSession session) = await SeedAsync(provider);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        IdentityDbContext owned = provider.GetRequiredService<IdentityDbContext>();
        await using (IDbContextTransaction transaction = await provider.GetRequiredService<SharedPersistenceDbContext>().Database.BeginTransactionAsync()) {
            shared.Users.Add(User.Create("identity-rollback@example.com", "hash"));
            await provider.GetRequiredService<IEmailTemplateRepository>().UpsertAsync("context-test", "en", "Rolled back", "<p>rollback</p>", "rollback", isActive: true);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            Assert.True(await provider.GetRequiredService<IRefreshTokenSessionWriteRepository>().TryRotateAsync(
                session.Id, user.Id, "initial", "rotated", rememberMe: true, DateTime.UtcNow));
            string ticket = await provider.GetRequiredService<ITelegramLoginTicketStore>().CreateAsync(
                "context-test", "browser", "synthetic payload", DateTime.UtcNow.AddMinutes(5), CancellationToken.None);
            Assert.Equal(43, ticket.Length);
            Assert.True(await provider.GetRequiredService<ITelegramAssertionReplayGuard>().TryConsumeAsync("synthetic assertion", DateTime.UtcNow.AddMinutes(5)));
            Assert.NotNull(await provider.GetRequiredService<ITelegramOperationStore>().RegisterAsync(123, 1, user.Id.Value, 0, "synthetic operation", CancellationToken.None));
            ITelegramOperationStore operations = provider.GetRequiredService<ITelegramOperationStore>();
            Guid operationId = Assert.Single(await operations.ListReadyAsync(123, CancellationToken.None));
            TelegramOperationLease? lease = await operations.AcquireAsync(123, operationId, CancellationToken.None);
            Assert.NotNull(lease);
            Assert.True(await operations.CheckpointAsync(123, operationId, lease.LeaseId, "checkpoint", completed: true, DateTime.UtcNow, CancellationToken.None));
            Assert.Same(transaction.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
            Assert.Equal("rotated", (await owned.UserRefreshTokenSessions.AsNoTracking().SingleAsync()).RefreshTokenHash);
            await transaction.RollbackAsync();
        }
        Assert.Single(await database.Users.ToListAsync());
        Assert.Equal("initial", (await database.UserRefreshTokenSessions.AsNoTracking().SingleAsync()).RefreshTokenHash);
        Assert.Equal("Original", (await database.EmailTemplates.AsNoTracking().SingleAsync(item => item.Key == "context-test" && item.Locale == "en")).Subject);
        Assert.Empty(await database.Set<TelegramLoginTicket>().ToListAsync());
        Assert.Empty(await database.Set<TelegramOperation>().ToListAsync());
        Assert.Empty(await database.Set<ConsumedTelegramAssertion>().ToListAsync());
    }

    [RequiresDockerFact]
    public async Task ConcurrentRefreshRotationHasOneWinnerThroughOwnerContextAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        string connectionString = database.Database.GetConnectionString()!;
        await using ServiceProvider first = CreateProvider(connectionString);
        await using ServiceProvider second = CreateProvider(connectionString);
        (User user, UserRefreshTokenSession session) = await SeedAsync(first);
        Task<bool> left = first.GetRequiredService<IRefreshTokenSessionWriteRepository>().TryRotateAsync(
            session.Id, user.Id, "initial", "left", rememberMe: true, DateTime.UtcNow);
        Task<bool> right = second.GetRequiredService<IRefreshTokenSessionWriteRepository>().TryRotateAsync(
            session.Id, user.Id, "initial", "right", rememberMe: true, DateTime.UtcNow);
        bool[] results = await Task.WhenAll(left, right);
        Assert.Equal(1, results.Count(value => value));
        UserRefreshTokenSession stored = await database.UserRefreshTokenSessions.AsNoTracking().SingleAsync();
        Assert.Contains(stored.RefreshTokenHash, new[] { "left", "right" }, StringComparer.Ordinal);
        Assert.Null(stored.PreviousRefreshTokenValidUntilUtc);
    }

    [RequiresDockerFact]
    public async Task SessionRevocationsAndRetentionRollBackWithSharedTransactionAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        (User user, UserRefreshTokenSession current) = await SeedAsync(provider);
        IRefreshTokenSessionReadRepository reads = provider.GetRequiredService<IRefreshTokenSessionReadRepository>();
        IRefreshTokenSessionWriteRepository writes = provider.GetRequiredService<IRefreshTokenSessionWriteRepository>();
        await using IDbContextTransaction transaction = await provider.GetRequiredService<SharedPersistenceDbContext>().Database.BeginTransactionAsync();
        Assert.Single(await reads.GetActiveByUserIdAsync(user.Id));
        Assert.Single(await provider.GetRequiredService<IRefreshTokenSessionReadModelRepository>().GetActiveReadModelsAsync(user.Id));
        await writes.UpdateAsync(current);
        UserRefreshTokenSession[] others = [.. Enumerable.Range(0, 3).Select(_ =>
            UserRefreshTokenSession.Create(Guid.NewGuid(), user.Id, "other", rememberMe: true, "password", ipAddress: null, userAgent: null, DateTime.UtcNow))];
        foreach (UserRefreshTokenSession session in others) { await writes.AddAsync(session); }
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await writes.RevokeByIdAsync(others[0].Id, user.Id, DateTime.UtcNow);
        await writes.RevokeOtherByIdAsync(others[1].Id, user.Id, current.Id, DateTime.UtcNow);
        await writes.RevokeAllOtherAsync(user.Id, current.Id, DateTime.UtcNow);
        Assert.Equal(current.Id, Assert.Single(await provider.GetRequiredService<IRefreshTokenSessionReadModelRepository>().GetActiveReadModelsAsync(user.Id)).Id);
        Assert.Equal(1, await provider.GetRequiredService<IUserLoginEventWriteRepository>().DeleteOlderThanAsync(DateTime.UtcNow.AddDays(1), 10));
        Assert.Same(transaction.GetDbTransaction(), provider.GetRequiredService<IdentityDbContext>().Database.CurrentTransaction!.GetDbTransaction());
        await transaction.RollbackAsync();
        Assert.Single(await database.UserRefreshTokenSessions.ToListAsync());
        Assert.Single(await database.UserLoginEvents.ToListAsync());
    }

    private static async Task<(User User, UserRefreshTokenSession Session)> SeedAsync(ServiceProvider provider) {
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        var user = User.Create("identity-context@example.com", "hash");
        var session = UserRefreshTokenSession.Create(Guid.NewGuid(), user.Id, "initial", rememberMe: true,
            "password", ipAddress: null, userAgent: null, DateTime.UtcNow);
        shared.Users.Add(user);
        await provider.GetRequiredService<IRefreshTokenSessionWriteRepository>().AddAsync(session);
        await provider.GetRequiredService<IUserLoginEventWriteRepository>().AddAsync(UserLoginEvent.Create(
            user.Id, "password", ipAddress: null, userAgent: null, browserName: null, browserVersion: null,
            operatingSystem: null, deviceType: null, DateTime.UtcNow));
        await provider.GetRequiredService<IEmailTemplateRepository>().UpsertAsync("context-test", "en", "Original", "<p>original</p>", "original", isActive: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return (user, session);
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "false",
        }).Build()).AddOutboxProcessing(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "false",
        }).Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddIdentityPersistence();
        services.AddReadModelComposition();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
