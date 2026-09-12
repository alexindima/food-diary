using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealRecognitionTransactionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
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
            var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));
            var receipts = new MealRecognitionReceiptRepository(context);
            return await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
                MealRecognitionReceipt? existing = await receipts.FindAsync(user.Id, operationId, cancellationToken);
                if (existing is not null) {
                    return existing.MealId;
                }
                var meal = Meal.Create(user.Id, now);
                context.Add(meal);
                uint version = await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
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
        var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteSerializedAsync<int>(user.Id, async cancellationToken => {
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            context.Add(meal);
            await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
            throw new InvalidOperationException("Simulated failure before receipt save.");
        }));

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.False(await context.Meals.AnyAsync(meal => meal.UserId == user.Id));
        Assert.False(await context.Set<MealRecognitionReceipt>().AnyAsync(receipt => receipt.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task FailureResultAfterMealFlush_RollsBackInsteadOfCommitting() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recognition-result-{Guid.NewGuid():N}@example.com", "hash");
        context.Add(user);
        await context.SaveChangesAsync();
        var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));

        Result result = await runner.ExecuteSerializedAsync(user.Id, async cancellationToken => {
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            context.Add(meal);
            await runner.FlushCreatedMealAsync(meal.Id, user.Id, cancellationToken);
            return Result.Failure(new Error("Meal.RecognitionConflict", "Recognition already consumed.", ErrorKind.Conflict));
        });

        Assert.True(result.IsFailure);
        Assert.Empty(context.ChangeTracker.Entries());
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
        var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));
        var receipts = new MealRecognitionReceiptRepository(context);

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
        var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));
        var receipts = new MealRecognitionReceiptRepository(context);

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
        var runner = new EfMealRecognitionTransactionRunner(context, new TestUnitOfWork(context));
        var receipts = new MealRecognitionReceiptRepository(context);

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

    [ExcludeFromCodeCoverage]
    private sealed class TestUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
        public bool HasPendingChanges => context.ChangeTracker.HasChanges();
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await context.SaveChangesAsync(cancellationToken);
    }
}
