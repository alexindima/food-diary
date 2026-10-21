using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Gamification.Application.Queries.GetGamification;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Gamification.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class InvalidCalendarZoneTests {
    [Fact]
    public async Task UnknownZone_IsRejectedBeforeMealStatisticsAchievementWorkOrClockAccess() {
        var user = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(user, cancellation.Token).Returns((Error?)null);
        IUserGamificationProfileReadService profiles = Substitute.For<IUserGamificationProfileReadService>();
        profiles.GetGamificationProfileAsync(user, cancellation.Token).Returns(Result.Success(new UserGamificationProfileModel(default)));
        ISender activity = Substitute.For<ISender>();
        IMealNutritionStatisticsReadService statistics = Substitute.For<IMealNutritionStatisticsReadService>();
        IAchievementMetricReader metrics = Substitute.For<IAchievementMetricReader>();
        IAchievementAwardService awards = Substitute.For<IAchievementAwardService>();
        TimeProvider clock = Substitute.For<TimeProvider>();
        var handler = new GetGamificationQueryHandler(activity, statistics, profiles, metrics, awards, clock, access);

        Error error = ResultAssert.Failure(await handler.Handle(
            new GetGamificationQuery(user.Value, TimeZoneId: "FoodDiary/Unknown"), cancellation.Token));

        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", error.Code),
            () => Assert.Contains("calendar time zone", error.Message, StringComparison.Ordinal),
            () => Assert.Empty(activity.ReceivedCalls()),
            () => Assert.Empty(statistics.ReceivedCalls()),
            () => Assert.Empty(metrics.ReceivedCalls()),
            () => Assert.Empty(awards.ReceivedCalls()),
            () => Assert.Empty(clock.ReceivedCalls()));
        await access.Received(1).EnsureCanAccessAsync(user, cancellation.Token);
        await profiles.Received(1).GetGamificationProfileAsync(user, cancellation.Token);
    }
}
