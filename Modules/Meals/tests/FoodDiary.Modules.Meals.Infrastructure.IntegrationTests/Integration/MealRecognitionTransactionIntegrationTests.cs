using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealRecognitionTransactionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task InvalidTransactionScope_CannotCaptureOrLockMealReceipt() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();
        var owner = UserId.New();
        var meal = MealId.New();
        await Assert.ThrowsAsync<ArgumentException>(() => runner.ExecuteSerializedAsync(new UserId(Guid.Empty), _ => Task.FromResult(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.FlushCreatedMealAsync(meal, owner));
        await Assert.ThrowsAsync<InvalidOperationException>(() => receipts.LockMealForUndoAsync(owner, meal));
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteSerializedAsync(owner, token => runner.FlushCreatedMealAsync(meal, owner, token)));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task ConcurrentRetries_CommitOneMealAndReceipt() {
        await using FoodDiaryDbContext setup = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-race-{Guid.NewGuid():N}@example.com", "hash");
        setup.Add(user);
        await setup.SaveChangesAsync();
        string connectionString = setup.Database.GetConnectionString()!;
        var operationId = Guid.NewGuid();
        var recognitionId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Task<MealId>[] attempts = [.. Enumerable.Range(0, 4).Select(async _ => {
            await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
            await using ServiceProvider provider = CreateProvider(context);
            MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
            IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
            IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();
            return await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
                MealRecognitionReceipt? existing = await receipts.FindAsync(user.Id, operationId, cancellationToken);
                if (existing is not null) {
                    return existing.MealId;
                }
                var meal = Meal.Create(user.Id, now);
                owned.Add(meal);
                uint version = await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
                Assert.Equal(5, owned.Model.GetEntityTypes().Count());
                Assert.Same(context.Database.GetDbConnection(), owned.Database.GetDbConnection());
                Assert.Empty(context.ChangeTracker.Entries<Meal>());
                Assert.NotEqual(0u, version);
                Assert.Null(await receipts.FindAsync(user.Id, operationId, cancellationToken));
                Assert.Equal(version, (await receipts.LockMealForUndoAsync(user.Id, meal.Id, cancellationToken))!.Value.Version);
                await receipts.AddAsync(MealRecognitionReceipt.Create(operationId, user.Id, recognitionId, meal.Id,
                    version, now, now, TimeSpan.FromHours(24)), cancellationToken);
                return meal.Id;
            });
        })];
        MealId[] result = await Task.WhenAll(attempts);

        Assert.Single(result.Distinct());
        Assert.Equal(1, await setup.Meals.CountAsync(meal => meal.UserId == user.Id));
        Assert.Equal(1, await setup.Set<MealRecognitionReceipt>().CountAsync(receipt => receipt.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task FailureAfterMealFlush_RollsBackMealAndClearsTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-rollback-{Guid.NewGuid():N}@example.com", "hash");
        context.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteSerializedAsync<int>(user.Id, async cancellationToken => {
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            owned.Add(meal);
            await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
            throw new InvalidOperationException("Simulated failure before receipt save.");
        }));

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.False(await context.Meals.AnyAsync(meal => meal.UserId == user.Id));
        Assert.False(await context.Set<MealRecognitionReceipt>().AnyAsync(receipt => receipt.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task FailureResultAfterMealFlush_RollsBackInsteadOfCommitting() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-result-{Guid.NewGuid():N}@example.com", "hash");
        context.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();

        Result result = await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            owned.Add(meal);
            await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
            return Result.Failure(new Error("Meal.RecognitionConflict", "Recognition already consumed.", ErrorKind.Conflict));
        });

        Assert.True(result.IsFailure);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.False(await context.Meals.AnyAsync(meal => meal.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task EditedMeal_HasDifferentPersistedVersion_AndCannotBeUndone() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-edited-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var meal = Meal.Create(user.Id, now);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        uint version = context.Entry(meal).Property<uint>("xmin").CurrentValue;
        var receipt = MealRecognitionReceipt.Create(Guid.NewGuid(), user.Id, Guid.NewGuid(), meal.Id,
            version, now, now, TimeSpan.FromHours(24));
        context.Add(receipt);
        await context.SaveChangesAsync();
        await using (FoodDiaryDbContext editor = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!)) {
            Meal edited = await editor.Meals.SingleAsync(candidate => candidate.Id == meal.Id);
            edited.UpdateComment("Corrected in the web diary");
            await editor.SaveChangesAsync();
        }
        await using ServiceProvider provider = CreateProvider(context);
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();

        await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
            MealRecognitionReceipt? persistedReceipt = await receipts.FindAsync(user.Id, receipt.OperationId, cancellationToken);
            Assert.NotNull(persistedReceipt);
            (Meal Meal, uint Version)? locked = await receipts.LockMealForUndoAsync(user.Id, meal.Id, cancellationToken);
            Assert.NotNull(locked);
            Assert.NotEqual(persistedReceipt.MealVersion, locked.Value.Version);
            Assert.Equal(MealRecognitionUndoResult.Changed, persistedReceipt.TryUndo(locked.Value.Version, DateTime.UtcNow));
            return true;
        });
        Assert.True(await context.Meals.AnyAsync(candidate => candidate.Id == meal.Id));
    }

    [RequiresDockerFact]
    public async Task UndoLock_ReturnsCurrentOwnedVersion_AndRejectsForeignOwner() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-lock-{Guid.NewGuid():N}@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.UtcNow);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        uint version = context.Entry(meal).Property<uint>("xmin").CurrentValue;
        await using ServiceProvider provider = CreateProvider(context);
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();

        await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
            Assert.Null(await receipts.LockMealForUndoAsync(new UserId(Guid.NewGuid()), meal.Id, cancellationToken));
            (Meal Meal, uint Version)? locked = await receipts.LockMealForUndoAsync(user.Id, meal.Id, cancellationToken);
            Assert.NotNull(locked);
            Assert.Equal(version, locked.Value.Version);
            Assert.Equal(meal.Id, locked.Value.Meal.Id);
            return true;
        });
    }

    [RequiresDockerFact]
    public async Task DeletedMeal_ReceiptCanBeMarkedUnavailableAndPersistsAcrossTransactions() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-deleted-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var meal = Meal.Create(user.Id, now);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        uint version = context.Entry(meal).Property<uint>("xmin").CurrentValue;
        var id = Guid.NewGuid();
        context.Add(MealRecognitionReceipt.Create(id, user.Id, id, meal.Id, version, now, now, TimeSpan.FromHours(24)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await context.Meals.Where(candidate => candidate.Id == meal.Id).ExecuteDeleteAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();

        await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
            MealRecognitionReceipt? receipt = await receipts.FindByRecognitionAsync(user.Id, id, cancellationToken);
            Assert.NotNull(receipt);
            Assert.Null(await receipts.LockMealForUndoAsync(user.Id, receipt.MealId, cancellationToken));
            Assert.Equal(MealRecognitionUndoResult.AlreadyDeleted, receipt.TryUndo(currentMealVersion: null, now.AddDays(2)));
            return true;
        });
        context.ChangeTracker.Clear();

        MealRecognitionReceipt? persisted = await receipts.FindAsync(user.Id, id);
        Assert.NotNull(persisted);
        Assert.NotNull(persisted.UndoneAtUtc);
        Assert.False(await context.Meals.AnyAsync(candidate => candidate.Id == meal.Id));
        Assert.Equal(MealRecognitionUndoResult.AlreadyUndone, persisted.TryUndo(currentMealVersion: null, now.AddDays(3)));
    }

    [RequiresDockerFact]
    public async Task CreatedMeal_CanBeUndoneAtomicallyThroughOwnerRepositories() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recognition-owner-undo@example.com", "hash");
        context.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRepository meals = provider.GetRequiredService<IMealRepository>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        var operationId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        MealId mealId = await runner.ExecuteSerializedAsync(user.Id, async token => {
            var meal = Meal.Create(user.Id, now);
            await meals.AddAsync(meal, token);
            uint version = await runner.FlushCreatedMealAsync(meal.Id, user.Id, token);
            Assert.NotNull(await meals.GetByIdAsync(meal.Id, user.Id, cancellationToken: token));
            await receipts.AddAsync(MealRecognitionReceipt.Create(operationId, user.Id, Guid.NewGuid(), meal.Id,
                version, now, now, TimeSpan.FromHours(24)), token);
            return meal.Id;
        });
        await runner.ExecuteSerializedAsync(user.Id, async token => {
            MealRecognitionReceipt? receipt = await receipts.FindAsync(user.Id, operationId, token);
            Assert.NotNull(receipt);
            (Meal Meal, uint Version)? locked = await receipts.LockMealForUndoAsync(user.Id, mealId, token);
            Assert.NotNull(locked);
            receipt.TryUndo(locked.Value.Version, now.AddMinutes(1));
            Assert.NotNull(receipt.UndoneAtUtc);
            await meals.DeleteAsync(locked.Value.Meal, token);
            return true;
        });
        owned.ChangeTracker.Clear();
        Assert.False(await context.Meals.AnyAsync(meal => meal.Id == mealId));
        Assert.NotNull((await receipts.FindAsync(user.Id, operationId))!.UndoneAtUtc);
        Assert.Empty(context.ChangeTracker.Entries<Meal>());
    }

    [RequiresDockerFact]
    public async Task CancellationAfterMealFlush_RollsBackAndAllowsNextTransaction() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recognition-cancellation@example.com", "hash");
        context.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.ExecuteSerializedAsync(user.Id, async token => {
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            owned.Add(meal);
            await runner.FlushCreatedMealAsync(meal.Id, user.Id, token);
            await cancellation.CancelAsync();
            token.ThrowIfCancellationRequested();
            return meal.Id;
        }, cancellation.Token));
        Assert.Multiple(() => Assert.Empty(owned.ChangeTracker.Entries()),
            () => Assert.Empty(context.ChangeTracker.Entries()),
            () => Assert.Null(context.Database.CurrentTransaction));
        Assert.False(await context.Meals.AnyAsync(meal => meal.UserId == user.Id));
        await runner.ExecuteSerializedAsync(user.Id, _ => {
            owned.Add(Meal.Create(user.Id, DateTime.UtcNow));
            return Task.FromResult(true);
        });
        Assert.Equal(1, await context.Meals.CountAsync(meal => meal.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task TransientFailureAfterFlush_RetriesWholeMealAndReceiptTransaction() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recognition-retry-flush@example.com", "hash");
        seed.Add(user);
        await seed.SaveChangesAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString(), postgres => postgres.EnableRetryOnFailure(2, TimeSpan.Zero, errorCodesToAdd: null)).Options;
        await using var context = new FoodDiaryDbContext(options);
        await using ServiceProvider provider = CreateProvider(context);
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        IMealRecognitionTransactionRunner runner = provider.GetRequiredService<IMealRecognitionTransactionRunner>();
        IMealRecognitionReceiptRepository receipts = provider.GetRequiredService<IMealRecognitionReceiptRepository>();
        var operationId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        int attempts = 0;
        MealId result = await runner.ExecuteSerializedAsync(user.Id, async token => {
            attempts++;
            Assert.Empty(owned.ChangeTracker.Entries());
            Assert.Null(await receipts.FindAsync(user.Id, operationId, token));
            var meal = Meal.Create(user.Id, now);
            owned.Add(meal);
            uint version = await runner.FlushCreatedMealAsync(meal.Id, user.Id, token);
            if (attempts == 1) {
                throw new Npgsql.NpgsqlException("Injected transient failure after meal flush.", new TimeoutException());
            }
            await receipts.AddAsync(MealRecognitionReceipt.Create(operationId, user.Id, operationId, meal.Id,
                version, now, now, TimeSpan.FromHours(24)), token);
            return meal.Id;
        });
        Assert.Equal(2, attempts);
        Assert.Equal(result, (await seed.Meals.SingleAsync(meal => meal.UserId == user.Id)).Id);
        Assert.Equal(result, (await seed.Set<MealRecognitionReceipt>().SingleAsync(receipt => receipt.UserId == user.Id)).MealId);
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = context.Database.GetConnectionString(),
            ["Database:MaxRetryDelaySeconds"] = "1",
        }).Build());
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddMealsPersistence();
        services.AddProductsPersistence();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
