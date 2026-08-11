using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Achievements;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class AchievementPersistenceTests {
    private static readonly DateTime Now = new(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AchievementEvaluationMessage_WithEmptyUserId_Throws() => Assert.Throws<ArgumentException>(() =>
        AchievementEvaluationOutboxMessage.Create(UserId.Empty, Now));

    [Fact]
    public void AchievementEvaluationMessage_Lifecycle_NormalizesAndClearsState() {
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now.ToLocalTime());

        message.MarkClaimed(Now.AddMinutes(1).ToLocalTime(), $"  {new string('w', 140)}  ");
        Assert.Multiple(
            () => Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind),
            () => Assert.Equal(128, message.LockedBy?.Length));

        message.MarkFailed($"  {new string('x', 2100)}  ", Now.AddMinutes(2).ToLocalTime());
        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.Equal(2048, message.LastError?.Length),
            () => Assert.Null(message.LockedBy));

        message.MarkDeadLettered(" ", Now.AddMinutes(3));
        Assert.Multiple(
            () => Assert.Equal(2, message.AttemptCount),
            () => Assert.NotNull(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LastError));

        message.MarkReplayed(Now.AddMinutes(4));
        Assert.Null(message.DeadLetteredOnUtc);
        message.MarkProcessed(Now.AddMinutes(5));
        Assert.Multiple(
            () => Assert.NotNull(message.ProcessedOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public async Task AchievementDefinitionStore_GetAllAsync_OrdersDefinitions() {
        await using FoodDiaryDbContext context = CreateContext();
        AchievementDefinition second = CreateDefinition("second", sortOrder: 2);
        AchievementDefinition firstB = CreateDefinition("b", sortOrder: 1);
        AchievementDefinition firstA = CreateDefinition("a", sortOrder: 1);
        context.AchievementDefinitions.AddRange(second, firstB, firstA);
        await context.SaveChangesAsync();

        IReadOnlyList<AchievementDefinition> result = await new AchievementDefinitionStore(context).GetAllAsync();

        Assert.Equal(["a", "b", "second"], result.Select(static item => item.Key), StringComparer.Ordinal);
    }

    [Fact]
    public void FoodDiaryDbContext_ExposesWeeklyGoalsSet() {
        using FoodDiaryDbContext context = CreateContext();

        DbSet<WeeklyGoal> goals = context.WeeklyGoals;

        Assert.NotNull(goals);
    }

    private static AchievementDefinition CreateDefinition(string key, int sortOrder) => AchievementDefinition.Create(
        key, "habits", AchievementMetric.TotalMeals, 10, "Title RU", "Title", "Description RU", "Description", "trophy", sortOrder);

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FoodDiaryDbContext(options);
    }
}
