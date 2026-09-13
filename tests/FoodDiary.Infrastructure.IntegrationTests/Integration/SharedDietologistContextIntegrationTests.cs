using System.Data.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Infrastructure;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedDietologistContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSavePersistsOwnerGraphAndAuditsOwnerOnlyChangesAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        DietologistDbContext owned = provider.GetRequiredService<DietologistDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var dietologist = User.Create("dietologist-owner@example.com", "hash");
        var client = User.Create("dietologist-client@example.com", "hash");
        shared.Users.AddRange(dietologist, client);
        var invitation = DietologistInvitation.Create(client.Id, dietologist.Email!, "hash", DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled);
        var recommendation = Recommendation.Create(dietologist.Id, client.Id, "Eat vegetables");
        var task = ClientTask.Create(dietologist.Id, client.Id, "Keep a diary", details: null, dueAtUtc: null);
        await provider.GetRequiredService<IDietologistInvitationWriteRepository>().AddAsync(invitation);
        await provider.GetRequiredService<IRecommendationWriteRepository>().AddAsync(recommendation);
        await provider.GetRequiredService<IClientTaskWriteRepository>().AddAsync(task);
        await provider.GetRequiredService<IRecommendationCommentWriteRepository>().AddAsync(RecommendationComment.Create(recommendation.Id, client.Id, "Thanks"));
        await provider.GetRequiredService<IRecommendationTemplateWriteRepository>().AddAsync(RecommendationTemplate.Create(dietologist.Id, "Template", "Text"));
        await provider.GetRequiredService<IRecommendationBulkDispatchWriteRepository>().AddAsync(RecommendationBulkDispatch.Create(dietologist.Id, client.Id, recommendation.Id, "batch"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await unitOfWork.SaveChangesAsync();

        Assert.Equal(6, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Equal(4, await database.AuditEntries.CountAsync());
        Assert.Single(await provider.GetRequiredService<IRecommendationReadModelRepository>().GetByClientReadModelsAsync(client.Id));
        Assert.Single(await provider.GetRequiredService<IRecommendationCommentReadModelRepository>().GetByRecommendationAsync(recommendation.Id));
        shared.ChangeTracker.Clear();
        owned.ChangeTracker.Clear();
        Recommendation? tracked = await provider.GetRequiredService<IRecommendationReadRepository>().GetByIdAsync(recommendation.Id, asTracking: true);
        Assert.NotNull(tracked);
        Assert.Empty(shared.ChangeTracker.Entries());
        tracked.MarkAsRead();
        await unitOfWork.SaveChangesAsync();
        Assert.True((await database.Recommendations.AsNoTracking().SingleAsync()).IsRead);
        Assert.Single(await database.AuditEntries.Where(entry => entry.Action == "dietologist.recommendation.read").ToListAsync());
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(5, await database.AuditEntries.CountAsync());
        DietologistInvitation? detached = await provider.GetRequiredService<IDietologistInvitationReadRepository>().GetByIdAsync(invitation.Id);
        Assert.NotNull(detached);
        detached.UpdatePermissions(new DietologistPermissions(ShareMeals: false));
        await provider.GetRequiredService<IDietologistInvitationWriteRepository>().UpdateAsync(detached);
        await unitOfWork.SaveChangesAsync();
        Assert.False((await database.DietologistInvitations.AsNoTracking().SingleAsync()).ShareMeals);
        Assert.Single(await database.AuditEntries.Where(entry => entry.Action == "dietologist.permissions.updated").ToListAsync());
    }

    [RequiresDockerFact]
    public async Task FailedOwnerSaveRollsBackAuditAndSharedWritesAndCanBeRetriedAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        var dietologist = User.Create("rollback-dietologist@example.com", "hash");
        var client = User.Create("rollback-client@example.com", "hash");
        shared.Users.Add(dietologist);
        await provider.GetRequiredService<IRecommendationWriteRepository>().AddAsync(Recommendation.Create(dietologist.Id, client.Id, "Advice"));
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
        Assert.Empty(await database.Users.ToListAsync());
        Assert.Empty(await database.AuditEntries.ToListAsync());
        Assert.Empty(await database.Recommendations.ToListAsync());
        shared.Users.Add(client);
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(2, await database.Users.CountAsync());
        Assert.Single(await database.Recommendations.ToListAsync());
        Assert.Single(await database.AuditEntries.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task TransientOwnerSaveRetriesWithoutDuplicatingAuditAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var fault = new TransientInsertFault();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, fault);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        var dietologist = User.Create("retry-dietologist@example.com", "hash");
        var client = User.Create("retry-client@example.com", "hash");
        shared.Users.AddRange(dietologist, client);
        await provider.GetRequiredService<IRecommendationWriteRepository>().AddAsync(Recommendation.Create(dietologist.Id, client.Id, "Advice"));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(2, fault.Attempts);
        Assert.Single(await database.Recommendations.ToListAsync());
        Assert.Single(await database.AuditEntries.ToListAsync());
    }

    private static ServiceProvider CreateProvider(string connectionString, DbCommandInterceptor? fault = null) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:MaxRetryDelaySeconds"] = "1",
        }).Build());
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddDietologistModule();
        services.AddReadModelComposition();
        if (fault is not null) {
            services.AddDbContext<FoodDiaryDbContext>(options => options.AddInterceptors(fault));
        }
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TransientInsertFault : DbCommandInterceptor {
        public int Attempts { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) {
            if (command.CommandText.Contains("INSERT INTO \"Recommendations\"", StringComparison.Ordinal) && ++Attempts == 1) {
                throw new TimeoutException("Simulated transient owner save failure");
            }
            return ValueTask.FromResult(result);
        }
    }
}
