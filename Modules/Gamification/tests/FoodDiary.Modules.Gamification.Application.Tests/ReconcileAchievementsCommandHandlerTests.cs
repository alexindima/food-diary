using FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;
using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
using FoodDiary.Modules.Gamification.Application.Commands.ReconcileAchievements;
using FoodDiary.Testing;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReconcileAchievementsCommandHandlerTests {
    [Fact]
    public async Task ReconcileAsync_UsesCompleteHistoryAndEvaluatesAwards() {
        var userId = UserId.New();
        ISender activity = Substitute.For<ISender>();
        activity.Send(Arg.Is<ReadDistinctMealDatesQuery>(q => q.UserId == userId && q.DateFrom == DateTime.UnixEpoch), Arg.Any<CancellationToken>())
            .Returns([new DateTime(2026, 8, 10), new DateTime(2026, 8, 9), new DateTime(2026, 8, 8)]);
        activity.Send(Arg.Is<ReadTotalMealCountQuery>(q => q.UserId == userId), Arg.Any<CancellationToken>()).Returns(10);
        IAchievementMetricReader metricReader = Substitute.For<IAchievementMetricReader>();
        metricReader.GetCompletedAcademyArticleCountAsync(userId, Arg.Any<CancellationToken>()).Returns(4);
        IAchievementAwardService awards = Substitute.For<IAchievementAwardService>();
        awards.EvaluateAndGrantAsync(userId, Arg.Any<AchievementMetricSnapshot>(), Arg.Any<CancellationToken>(), Arg.Any<DateTime?>())
            .Returns(Task.FromResult<IReadOnlyList<BadgeModel>>([]));
        FoodDiary.Mediator.ISender handler = RequestTestSender.Create(new ReconcileAchievementsCommandHandler(activity, metricReader, awards, new StubTimeProvider()));

        DateTime occurredAtUtc = new(2026, 8, 10, 11, 30, 0, DateTimeKind.Utc);
        await handler.Send(new ReconcileAchievementsCommand(userId, occurredAtUtc));

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
