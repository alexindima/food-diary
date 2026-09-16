using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;

using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
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
public sealed class SharedGamificationContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task StoresAndOutboxJoinSharedTransactionAndRollBackAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("achievement-transaction@example.com", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        await using IDbContextTransaction transaction = await provider.GetRequiredService<SharedPersistenceDbContext>().Database.BeginTransactionAsync();
        IAchievementDefinitionStore definitions = provider.GetRequiredService<IAchievementDefinitionStore>();
        IAchievementDefinitionReadModelRepository reads = provider.GetRequiredService<IAchievementDefinitionReadModelRepository>();
        Assert.Same(definitions, reads);
        Assert.NotEmpty(await definitions.GetAllAsync());
        IUserAchievementStore achievements = provider.GetRequiredService<IUserAchievementStore>();
        Assert.Empty(await achievements.GetByUserIdAsync(user.Id));
        IAchievementEvaluationOutbox outbox = provider.GetRequiredService<IAchievementEvaluationOutbox>();
        await outbox.EnqueueAsync(user.Id);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await outbox.EnqueueAsync(user.Id);
        GamificationDbContext owned = provider.GetRequiredService<GamificationDbContext>();
        Assert.Equal(2, (await owned.AchievementEvaluationOutbox.SingleAsync()).Revision);
        Assert.Same(transaction.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
        await transaction.RollbackAsync();
        Assert.Empty(await database.AchievementEvaluationOutbox.ToListAsync());
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
        ModuleRegistration.AddGamificationModule(services);

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
