using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedAiContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task UsageAndPromptRevisionsShareSaveAndRollbackAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        AiDbContext owned = provider.GetRequiredService<AiDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        IAiUsageRepository usages = provider.GetRequiredService<IAiUsageRepository>();
        IAiPromptTemplateRepository templates = provider.GetRequiredService<IAiPromptTemplateRepository>();
        var user = User.Create("ai-context@example.com", "hash");
        shared.Users.Add(user);
        await usages.AddAsync(AiUsage.Create(user.Id, "vision", "test", 2, 3, 5));
        AiPromptTemplate prompt = await templates.AddAsync(AiPromptTemplate.Create("context-test", "en", "Original"));
        Assert.Equal(6, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<AiUsage>());
        Assert.Empty(shared.ChangeTracker.Entries<AiPromptTemplate>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await unitOfWork.SaveChangesAsync();
        Assert.Single(await database.AiUsages.ToListAsync());
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
            shared.Users.Add(User.Create("ai-rollback@example.com", "hash"));
            await usages.AddAsync(AiUsage.Create(user.Id, "vision", "test", 10, 20, 30));
            await unitOfWork.SaveChangesAsync();
            AiPromptTemplate? tracked = await provider.GetRequiredService<IAiPromptTemplateWriteRepository>().GetByIdAsync(prompt.Id, asTracking: true);
            Assert.NotNull(tracked);
            Assert.Same(transaction.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
            tracked.Update("Changed", isActive: true);
            await templates.UpdateAsync(tracked);
            await unitOfWork.SaveChangesAsync();
            Assert.Single(await templates.GetRevisionsAsync("context-test", "en", CancellationToken.None));
            await transaction.RollbackAsync();
        }
        Assert.Single(await database.Users.ToListAsync());
        Assert.Single(await database.AiUsages.ToListAsync());
        Assert.Equal("Original", (await database.AiPromptTemplates.SingleAsync(item => item.Id == prompt.Id)).PromptText);
        Assert.Empty(await database.AiPromptTemplates.AsNoTracking().Where(item => item.Id == prompt.Id).SelectMany(item => item.Revisions).ToListAsync());
        Assert.False(database.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task QuotaCommitRemainsIndependentOfSharedRollbackAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("ai-independent@example.com", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        AiDbContext scoped = provider.GetRequiredService<AiDbContext>();
        await using var independent = new AiDbContext(provider.GetRequiredService<DbContextOptions<AiDbContext>>());
        Assert.NotSame(shared.Database.GetDbConnection(), independent.Database.GetDbConnection());
        Assert.Same(shared.Database.GetDbConnection(), scoped.Database.GetDbConnection());
        DateTime now = DateTime.UtcNow;
        var request = new AiQuotaReservationRequest(new string('a', 64), user.Id, new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), "nutrition", 10, 20, 100, 100, now.AddMinutes(15));
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
            shared.Users.Add(User.Create("ai-discarded@example.com", "hash"));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            AiQuotaReservationStatus result = await provider.GetRequiredService<IAiQuotaRepository>().ReserveAsync(request);
            Assert.Equal(AiQuotaReservationStatus.Acquired, result);
            await transaction.RollbackAsync();
        }
        Assert.Single(await database.Users.ToListAsync());
        Assert.Single(await database.AiQuotaReservations.ToListAsync());
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "false",
        }).Build());
        services.AddAiPersistence();
        services.AddReadModelComposition();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
