using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealRecognitionReceiptIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Receipt_SurvivesMealDeletion_ButIsRemovedWithOwner() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"receipt-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var meal = Meal.Create(user.Id, now);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        uint version = context.Entry(meal).Property<uint>("xmin").CurrentValue;
        var receipt = MealRecognitionReceipt.Create(Guid.NewGuid(), user.Id, Guid.NewGuid(), meal.Id,
            version, now, now, TimeSpan.FromHours(24));
        context.Add(receipt);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await context.Meals.Where(entry => entry.Id == meal.Id).ExecuteDeleteAsync();
        MealRecognitionReceipt persisted = await context.Set<MealRecognitionReceipt>().SingleAsync(entry => entry.OperationId == receipt.OperationId);
        Assert.Equal(version, persisted.MealVersion);
        Assert.Equal(MealRecognitionUndoResult.AlreadyDeleted, persisted.TryUndo(currentMealVersion: null, DateTime.UtcNow));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.NotNull((await context.Set<MealRecognitionReceipt>().SingleAsync(entry => entry.OperationId == receipt.OperationId)).UndoneAtUtc);

        await context.Users.Where(entry => entry.Id == user.Id).ExecuteDeleteAsync();
        Assert.False(await context.Set<MealRecognitionReceipt>().AnyAsync(entry => entry.OperationId == receipt.OperationId));
    }

    [RequiresDockerFact]
    public async Task Recognition_CannotBeSavedUnderAnotherOperationForSameOwner() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"receipt-unique-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var meal = Meal.Create(user.Id, now);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        uint version = context.Entry(meal).Property<uint>("xmin").CurrentValue;
        var recognitionId = Guid.NewGuid();
        context.Add(MealRecognitionReceipt.Create(Guid.NewGuid(), user.Id, recognitionId, meal.Id,
            version, now, now, TimeSpan.FromHours(24)));
        await context.SaveChangesAsync();
        context.Add(MealRecognitionReceipt.Create(Guid.NewGuid(), user.Id, recognitionId, meal.Id,
            version, now, now, TimeSpan.FromHours(24)));

        DbUpdateException error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Npgsql.PostgresException postgresError = Assert.IsType<Npgsql.PostgresException>(error.InnerException);
        Assert.Equal(Npgsql.PostgresErrorCodes.UniqueViolation, postgresError.SqlState);
        Assert.Equal("IX_MealRecognitionReceipts_UserId_RecognitionId", postgresError.ConstraintName);
    }
}
