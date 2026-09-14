using FoodDiary.Modules.Ai.Infrastructure;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.Application;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedAiContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task PromptRevisionsShareSaveAndRollbackAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        AiDbContext owned = provider.GetRequiredService<AiDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        Assert.Null(provider.GetService<IAiUsageQuery>());
        IAiPromptTemplateWriteRepository templates = provider.GetRequiredService<IAiPromptTemplateWriteRepository>();
        IAiPromptTemplateReadModelRepository templateReads = provider.GetRequiredService<IAiPromptTemplateReadModelRepository>();
        Assert.Same(templates, templateReads);
        var user = User.Create("ai-context@example.com", "hash");
        shared.Users.Add(user);
        AiPromptTemplate prompt = await templates.AddAsync(AiPromptTemplate.Create("context-test", "en", "Original"));
        Assert.Equal(6, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<AiUsage>());
        Assert.Empty(shared.ChangeTracker.Entries<AiPromptTemplate>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await unitOfWork.SaveChangesAsync();
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
            shared.Users.Add(User.Create("ai-rollback@example.com", "hash"));
            await unitOfWork.SaveChangesAsync();
            AiPromptTemplate? tracked = await provider.GetRequiredService<IAiPromptTemplateWriteRepository>().GetByKeyAsync("context-test", "en");
            Assert.NotNull(tracked);
            Assert.Same(transaction.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
            FoodDiary.Results.Result<AiPromptTemplateReadModel> changed = await provider.GetRequiredService<ISender>()
                .Send(new UpsertAiPromptCommand("context-test", "en", "Changed", IsActive: true));
            Assert.True(changed.IsSuccess);
            Assert.Equal(EntityState.Modified, owned.Entry(tracked).State);
            await unitOfWork.SaveChangesAsync();
            Assert.Single(await templateReads.GetRevisionsAsync("context-test", "en", CancellationToken.None));
            await transaction.RollbackAsync();
        }
        Assert.Single(await database.Users.ToListAsync());
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

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task JobCommitRemainsIndependentOfSharedRollbackAsync(bool resolveBeforeTransaction) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("ai-job-independent@example.com", "hash");
        var image = ImageAsset.Create(user.Id, "independent-job/image.jpg", "https://example.com/image.jpg");
        image.Confirm();
        database.Users.Add(user);
        database.ImageAssets.Add(image);
        await database.SaveChangesAsync();
        string connectionString = database.Database.GetConnectionString()!;
        await using ServiceProvider provider = CreateProvider(connectionString);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        IFoodRecognitionJobStore? store = resolveBeforeTransaction ? provider.GetRequiredService<IFoodRecognitionJobStore>() : null;
        DateTime now = DateTime.UtcNow;
        var job = new FoodRecognitionJobModel(Guid.NewGuid(), user.Id.Value, image.Id.Value, image.Url,
            "Independent admission", "Queued", now, now);
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
            shared.Users.Add(User.Create("ai-job-discarded@example.com", "hash"));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            store ??= provider.GetRequiredService<IFoodRecognitionJobStore>();
            Assert.Same(store, provider.GetRequiredService<IFoodRecognitionJobReader>());
            Assert.True((await store.CreateAsync(job, CancellationToken.None)).IsSuccess);
            Assert.Same(transaction, shared.Database.CurrentTransaction);
            await transaction.RollbackAsync();
        }

        Assert.Single(await database.Users.AsNoTracking().ToListAsync());
        await using ServiceProvider readerProvider = CreateProvider(connectionString);
        FoodRecognitionJobModel? persisted = await readerProvider.GetRequiredService<IFoodRecognitionJobReader>()
            .GetAsync(user.Id.Value, job.Id, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Multiple(
            () => Assert.Equal(job.Id, persisted.Id),
            () => Assert.Equal("Queued", persisted.Status));
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "false",
        }).Build());
        services.AddAiPersistence();
        services.AddAiApplication();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
