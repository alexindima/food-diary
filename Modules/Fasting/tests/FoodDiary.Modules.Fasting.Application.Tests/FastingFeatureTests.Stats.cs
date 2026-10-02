using FoodDiary.Modules.Fasting.Application.Queries.GetFastingStats;
using FoodDiary.Modules.Fasting.Application.Services;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Fasting.Application.Tests;

public partial class FastingFeatureTests {
    [Theory]
    [InlineData(new int[] { 0, 0, 1, 2 }, 3)]
    [InlineData(new int[] { 1, 1, 2, 3 }, 3)]
    [InlineData(new int[] { 0, 1, 1, 2 }, 3)]
    [InlineData(new int[] { 0, 0, 2 }, 1)]
    [InlineData(new int[] { 0, 2 }, 1)]
    [InlineData(new int[] { 1, 3 }, 1)]
    [InlineData(new int[] { 0, 1, 3 }, 2)]
    [InlineData(new int[] { 2, 2, 3 }, 0)]
    [InlineData(new int[] { 0, 0 }, 1)]
    public async Task GetFastingStats_WithMultipleSessionsPerDay_CountsConsecutiveDistinctDays(
        int[] daysAgo, int expectedStreak) {
        var userId = UserId.New();
        var repository = new InMemoryFastingOccurrenceRepository();
        var sessionsPerDay = new Dictionary<int, int>();
        foreach (int day in daysAgo) {
            int sessionIndex = sessionsPerDay.GetValueOrDefault(day);
            sessionsPerDay[day] = sessionIndex + 1;
            DateTime started = FixedNow.Date.AddDays(-day).AddHours(2 + (sessionIndex * 3));
            var occurrence = FastingOccurrence.Create(
                FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, started, 1, 2);
            occurrence.Complete(started.AddHours(2));
            repository.StoredOccurrences.Add(occurrence);
        }
        var handler = new GetFastingStatsQueryHandler(
            new FastingAnalyticsService(repository, new InMemoryFastingCheckInRepository()),
            CreateCurrentUserAccessService(userId), new FixedDateTimeProvider());

        Result<FastingStatsModel> result = await handler.Handle(
            new GetFastingStatsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(expectedStreak, result.Value.CurrentStreak),
            () => Assert.Equal(daysAgo.Length, result.Value.TotalCompleted),
            () => Assert.Equal(2, result.Value.AverageDurationHours),
            () => Assert.Equal(100, result.Value.CompletionRateLast30Days));
    }
}
