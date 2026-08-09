using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Gamification;

[ExcludeFromCodeCoverage]
public sealed class AchievementReconciliationHandlerTests {
    [Fact]
    public async Task ReconcileAsync_UsesCompleteHistoryAndEvaluatesAwards() {
        var userId = UserId.New();
        IMealActivityReadService activity = Substitute.For<IMealActivityReadService>();
        activity.GetDistinctMealDatesAsync(userId, DateTime.UnixEpoch, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([new DateTime(2026, 8, 10), new DateTime(2026, 8, 9), new DateTime(2026, 8, 8)]);
        activity.GetTotalMealCountAsync(userId, Arg.Any<CancellationToken>()).Returns(10);
        IAchievementMetricReader metricReader = Substitute.For<IAchievementMetricReader>();
        metricReader.GetCompletedAcademyArticleCountAsync(userId, Arg.Any<CancellationToken>()).Returns(4);
        IAchievementAwardService awards = Substitute.For<IAchievementAwardService>();
        awards.EvaluateAndGrantAsync(userId, Arg.Any<AchievementMetricSnapshot>(), Arg.Any<CancellationToken>(), Arg.Any<DateTime?>())
            .Returns(Task.FromResult<IReadOnlyList<BadgeModel>>([]));
        var handler = new AchievementReconciliationHandler(activity, metricReader, awards, new StubTimeProvider());

        DateTime occurredAtUtc = new(2026, 8, 10, 11, 30, 0, DateTimeKind.Utc);
        await handler.ReconcileAsync(userId, occurredAtUtc);

        await awards.Received(1).EvaluateAndGrantAsync(
            userId,
            new AchievementMetricSnapshot(3, 10, 4),
            Arg.Any<CancellationToken>(),
            occurredAtUtc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    }
}
