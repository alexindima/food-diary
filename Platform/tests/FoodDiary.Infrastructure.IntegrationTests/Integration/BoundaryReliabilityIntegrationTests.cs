using FoodDiary.Modules.Recipes.Infrastructure.Persistence.Recipes;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages;
using FoodDiary.Modules.Images.Application;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.ReadModel.Composition;
using FoodDiary.ReadModel.Composition.Images;
using System.Data.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Infrastructure.Persistence.Images;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BoundaryReliabilityIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData("Products", false)]
    [InlineData("Products", true)]
    [InlineData("Recipes", false)]
    [InlineData("Recipes", true)]
    [InlineData("WeeklyGoals", false)]
    [InlineData("WeeklyGoals", true)]
    [InlineData("Billing", false)]
    [InlineData("Billing", true)]
    [InlineData("BatchItem", false)]
    [InlineData("BatchItem", true)]
    public async Task Retry_DiscardsFailedAttemptEntitiesOutboxAndCallbacks(string owner, bool afterSave) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("retry-boundary@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        var weekStartUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        var fault = new RetryFault(afterSave);
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString(), provider => provider.EnableRetryOnFailure(2, TimeSpan.Zero, errorCodesToAdd: null))
            .AddInterceptors(new SaveFaultInterceptor(fault), new CommitFaultInterceptor(fault)).Options;
        await using var context = new FoodDiaryDbContext(options);
        await using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence.WeeklyGoalsDbContext? weeklyOwner = owner.Equals("WeeklyGoals", StringComparison.Ordinal)
            ? context.CreateModuleContext<FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence.WeeklyGoalsDbContext>(options => new FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence.WeeklyGoalsDbContext(options)) : null;
        IUnitOfWork unitOfWork = weeklyOwner is null ? new TestUnitOfWork(context)
            : new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance);
        var queue = new RecordingActionQueue();
        int attempts = 0;
        int delivered = 0;
        if (owner.Equals("BatchItem", StringComparison.Ordinal)) {
            queue.Enqueue("existing", _ => { delivered++; return Task.CompletedTask; });
        }
        async Task<Result> MutateAsync(CancellationToken cancellationToken) {
            attempts++;
            if (weeklyOwner is not null) {
                weeklyOwner.WeeklyGoals.Add(FoodDiary.Modules.WeeklyGoals.Domain.Entities.WeeklyGoal.Create(user.Id, weekStartUtc,
                    WeeklyGoalType.DiaryLogging, targetDays: 5, reminderEnabled: false, reminderTimeMinutes: null, timeZoneOffsetMinutes: null));
            }
            context.Products.Add(CreateProduct(user.Id, "one product"));
            await new ImageObjectDeletionOutbox(context.ImageObjectDeletionOutbox, TimeProvider.System).EnqueueAsync("one-object", isConfirmed: true, cancellationToken);
            if (!owner.Equals("BatchItem", StringComparison.Ordinal)) {
                queue.Enqueue("notify", _ => { delivered++; return Task.CompletedTask; });
            }
            return Result.Success();
        }

        switch (owner) {
            case "BatchItem":
                await new EfModuleTransactionCoordinator(context, unitOfWork, queue).ExecuteItemAsync(async (_, token) => {
                    await MutateAsync(token);
                    return true;
                });
                break;
            case "Products":
                await new EfProductMutationTransactionRunner(new EfModuleTransactionCoordinator(context, unitOfWork, queue)).ExecuteAsync(MutateAsync);
                break;
            case "Recipes":
                await new EfRecipeMutationTransactionRunner(new EfModuleTransactionCoordinator(context, unitOfWork, queue)).ExecuteAsync(MutateAsync);
                break;
            case "WeeklyGoals":
                await new EfWeeklyGoalTransactionRunner(new FoodDiary.Persistence.Runtime.Persistence.Shared.EfModuleTransactionCoordinator(context, unitOfWork, queue)).ExecuteSerializedAsync(user.Id, weekStartUtc, MutateAsync);
                break;
            case "Billing":
                await new EfBillingTransactionRunner(new EfModuleTransactionCoordinator(context, new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance), queue)).ExecuteAsync(async token => await MutateAsync(token));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(owner));
        }

        await queue.FlushAsync();
        Assert.Equal(2, attempts);
        Assert.Equal(1, delivered);
        Assert.Equal(1, await context.Products.AsNoTracking().CountAsync());
        Assert.Equal(1, await context.ImageObjectDeletionOutbox.AsNoTracking().CountAsync());
        Assert.False(context.ChangeTracker.HasChanges());
        if (weeklyOwner is not null) {
            Assert.Equal(1, await weeklyOwner.WeeklyGoals.AsNoTracking().CountAsync());
            Assert.False(weeklyOwner.ChangeTracker.HasChanges());
        }
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchItem_SavesOnlyProcessedItemsEvenWithoutTrackedChanges(bool processed) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: false);
        using var cancellation = new CancellationTokenSource();
        var coordinator = new EfModuleTransactionCoordinator(context, unitOfWork);
        Assert.Equal(processed, await coordinator.ExecuteItemAsync((transaction, token) => {
            Assert.Same(context.Database.CurrentTransaction!.GetDbTransaction(), transaction);
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(processed);
        }, cancellation.Token));
        await unitOfWork.Received(processed ? 1 : 0).SaveChangesAsync(cancellation.Token);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [RequiresDockerFact]
    public async Task ImageDeletion_RacingCommittedReference_RollsBackDeletionAndOutbox() {
        await using FoodDiaryDbContext deleting = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("image-race@example.com", "hash");
        var image = ImageAsset.Create(user.Id, "race", "https://example.com/race");
        image.Confirm();
        deleting.Users.Add(user);
        deleting.ImageAssets.Add(image);
        await deleting.SaveChangesAsync();
        deleting.ChangeTracker.Clear();
        var repository = new ImageAssetRepository(deleting.ImageAssets, new ImageAssetUsageQuery(deleting));
        Assert.False(await repository.IsAssetInUseAsync(image.Id));
        await repository.DeleteAsync(image);
        await new ImageObjectDeletionOutbox(deleting.ImageObjectDeletionOutbox, TimeProvider.System).EnqueueAsync(image.ObjectKey, isConfirmed: true);
        await using FoodDiaryDbContext linking = databaseFixture.CreateDbContext(deleting.Database.GetConnectionString()!);
        linking.Products.Add(CreateProduct(user.Id, "linked", image.Id));
        await linking.SaveChangesAsync();

        DbUpdateException failure = await Assert.ThrowsAsync<DbUpdateException>(() => deleting.SaveChangesAsync());
        PostgresException postgres = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
        Assert.Equal(image.Id, await linking.Products.AsNoTracking().Select(product => product.ImageAssetId).SingleAsync());
        Assert.True(await linking.ImageAssets.AsNoTracking().AnyAsync(asset => asset.Id == image.Id));
        Assert.Empty(await linking.ImageObjectDeletionOutbox.AsNoTracking().ToListAsync());
    }

    [RequiresDockerFact]
    public async Task ImageDeletion_WithTrackedReference_DoesNotMutateForeignAggregate() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("tracked-image@example.com", "hash");
        var image = ImageAsset.Create(user.Id, "tracked", "https://example.com/tracked");
        Product product = CreateProduct(user.Id, "tracked product", image.Id);
        context.Users.Add(user);
        context.ImageAssets.Add(image);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.ImageAssets.Remove(image);

        Assert.Equal(image.Id, product.ImageAssetId);
        Assert.Equal(EntityState.Unchanged, context.Entry(product).State);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(image.Id, await context.Products.AsNoTracking().Select(item => item.ImageAssetId).SingleAsync());
        Assert.True(await context.ImageAssets.AsNoTracking().AnyAsync(item => item.Id == image.Id));
    }

    [RequiresDockerFact]
    public async Task OrphanCleanup_FailedSave_DoesNotContaminateNextScope() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("image-batch@example.com", "hash");
        var first = ImageAsset.Create(user.Id, "first", "https://example.com/first");
        var second = ImageAsset.Create(user.Id, "second", "https://example.com/second");
        seed.Users.Add(user);
        seed.ImageAssets.AddRange(first, second);
        await seed.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(provider => provider.GetRequiredService<FoodDiaryDbContext>());
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseNpgsql(seed.Database.GetConnectionString())
            .AddInterceptors(new RejectImageDeleteInterceptor(first.Id)));
        services.AddScoped<SharedPersistenceDbContext>(provider => provider.GetRequiredService<FoodDiaryDbContext>());
        services.AddImagesModule().AddImagesInfrastructure();
        services.AddReadModelComposition();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<CleanupOrphanImagesCommandHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        int removed = await scope.ServiceProvider.GetRequiredService<CleanupOrphanImagesCommandHandler>()
            .Handle(new CleanupOrphanImagesCommand(DateTime.UtcNow.AddDays(1), 10), CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.Equal(first.Id, await seed.ImageAssets.AsNoTracking().Select(asset => asset.Id).SingleAsync());
        Assert.Equal(2, await seed.ImageObjectDeletionOutbox.AsNoTracking().CountAsync());
        Assert.All(await seed.ImageObjectDeletionOutbox.AsNoTracking().ToListAsync(), message => Assert.Equal("second", message.ObjectKey));
    }

    private static Product CreateProduct(UserId userId, string name, ImageAssetId? imageAssetId = null) =>
        Product.Create(userId, name, MeasurementUnit.G, 100, 100, 52, 1, 1, 11, 2, 0, imageAssetId: imageAssetId);

    [ExcludeFromCodeCoverage]
    private sealed class TestUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
        public bool HasPendingChanges => context.ChangeTracker.HasChanges();
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingActionQueue : IPostCommitActionQueue {
        private readonly List<Func<CancellationToken, Task>> _actions = [];
        public bool HasActions => _actions.Count > 0;
        public void Discard() => _actions.Clear();
        public void Enqueue(string actionName, Func<CancellationToken, Task> action) => _actions.Add(action);
        public async Task FlushAsync(CancellationToken cancellationToken = default) {
            foreach (Func<CancellationToken, Task> action in _actions) { await action(cancellationToken); }
            _actions.Clear();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RetryFault(bool afterSave) {
        private bool _raised;
        public void ThrowOnce(bool committing) {
            if (_raised || committing != afterSave) { return; }
            _raised = true;
            throw new TimeoutException("Injected transient failure in the first attempt.");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SaveFaultInterceptor(RetryFault fault) : SaveChangesInterceptor {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            fault.ThrowOnce(committing: false);
            return ValueTask.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CommitFaultInterceptor(RetryFault fault) : DbTransactionInterceptor {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) {
            fault.ThrowOnce(committing: true);
            return ValueTask.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RejectImageDeleteInterceptor(ImageAssetId failedId) : DbCommandInterceptor {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (eventData.Context!.ChangeTracker.Entries<ImageAsset>().Any(entry => entry.Entity.Id == failedId && entry.State == EntityState.Deleted)) {
                throw new InvalidOperationException("Injected image deletion failure.");
            }
            return ValueTask.FromResult(result);
        }
    }
}
