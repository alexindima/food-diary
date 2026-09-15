using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Infrastructure;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
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
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
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
        }).Build());
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
