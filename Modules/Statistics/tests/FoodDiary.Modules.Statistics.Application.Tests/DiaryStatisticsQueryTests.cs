using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval;
using FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetDiaryStatistics;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DiaryStatisticsQueryTests {
    [Fact]
    public async Task CorruptSystemTimeZone_DoesNotQueryNutritionOrHydration() {
        var owner = new UserId(Guid.NewGuid());
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        ISender statistics = Substitute.For<ISender>();
        ISender hydration = Substitute.For<ISender>();
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, global::FoodDiary.Testing.RequestTestSender.Route((statistics, [typeof(global::FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics.ReadDashboardStatisticsQuery)]), (hydration, [typeof(global::FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery)])), new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.HandleAsync(new GetDiaryStatisticsQuery(owner.Value, 1),
            _ => throw new InvalidTimeZoneException("Corrupt system rules."), CancellationToken.None);

        Assert.Equal("Statistics.InvalidTimeZone", result.Error.Code);
        Assert.Empty(statistics.ReceivedCalls());
        Assert.Empty(hydration.ReceivedCalls());
    }

    [Theory]
    [InlineData("access", "User.AccessDenied")]
    [InlineData("days", "Statistics.InvalidDays")]
    [InlineData("profile", "Profile.Unavailable")]
    [InlineData("owner", "Statistics.ProfileNotFound")]
    [InlineData("zone", "Statistics.InvalidTimeZone")]
    public async Task InvalidRequestOrProfile_DoesNotQueryNutritionOrHydration(string scenario, string expectedCode) {
        var owner = new UserId(Guid.NewGuid());
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        if (string.Equals(scenario, "access", StringComparison.Ordinal)) {
            access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns(new Error("User.AccessDenied", "Denied"));
        }
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        UserDashboardProfileModel profile = Profile(owner) with {
            Id = string.Equals(scenario, "owner", StringComparison.Ordinal) ? Guid.NewGuid() : owner.Value,
            TimeZoneId = string.Equals(scenario, "zone", StringComparison.Ordinal) ? "Unknown/Zone" : "UTC",
        };
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(string.Equals(scenario, "profile", StringComparison.Ordinal)
            ? Result.Failure<UserDashboardProfileModel>(new Error("Profile.Unavailable", "Unavailable")) : Result.Success(profile));
        ISender statistics = Substitute.For<ISender>();
        ISender hydration = Substitute.For<ISender>();
        var handler = new GetDiaryStatisticsQueryHandler(access, profiles, global::FoodDiary.Testing.RequestTestSender.Route((statistics, [typeof(global::FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics.ReadDashboardStatisticsQuery)]), (hydration, [typeof(global::FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery)])), new Clock());
        var query = new GetDiaryStatisticsQuery(owner.Value,
            string.Equals(scenario, "days", StringComparison.Ordinal) ? 2 : 1);
        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(query, CancellationToken.None);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Empty(statistics.ReceivedCalls());
        Assert.Empty(hydration.ReceivedCalls());
    }

    [Fact]
    public async Task SevenDays_UsesLocalBoundariesAndIncludesEmptyDaysInAverage() {
        var owner = new UserId(Guid.NewGuid());
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        ISender statistics = Substitute.For<ISender>();
        statistics.Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == owner && query.QuantizationDays == 2), Arg.Any<CancellationToken>()).Returns(call => {
            ReadDashboardStatisticsQuery request = call.Arg<ReadDashboardStatisticsQuery>();
            DateTime from = request.DateFrom;
            DateTime to = request.DateTo;
            bool recorded = from.Day == 4;
            return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                new(from, to, recorded ? 700 : 0, 0, 0, 0, 0, TotalProteins: recorded ? 70 : 0, MealCount: recorded ? 2 : 0),
            ]);
        });
        ISender hydration = Substitute.For<ISender>();
        hydration.Send(Arg.Is<ReadHydrationIntervalQuery>(q => q.UserId == owner), Arg.Any<CancellationToken>()).Returns(250L);
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, global::FoodDiary.Testing.RequestTestSender.Route((statistics, [typeof(global::FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics.ReadDashboardStatisticsQuery)]), (hydration, [typeof(global::FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery)])), new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(new GetDiaryStatisticsQuery(owner.Value, 7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.CalendarDays);
        Assert.Equal(1, result.Value.DaysWithMeals);
        Assert.Equal(100, result.Value.AverageCaloriesPerCalendarDay);
        Assert.Equal(70, result.Value.TotalProteins);
        Assert.Equal(1750, result.Value.TotalWaterMl);
        Assert.Equal(2500, result.Value.Days[4].CalorieGoal);
        await statistics.Received(1).Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == owner && query.DateFrom == new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc) && query.DateTo == new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc).AddTicks(-10) && query.QuantizationDays == 2), Arg.Any<CancellationToken>());
        await hydration.Received(1).Send(new ReadHydrationIntervalQuery(owner, new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadFailure_IsNotReportedAsAnEmptyDiary() {
        var owner = new UserId(Guid.NewGuid());
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        ISender statistics = Substitute.For<ISender>();
        statistics.Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == owner && query.QuantizationDays == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(new Error("Statistics.Unavailable", "Unavailable.", ErrorKind.Internal)));
        ISender hydration = Substitute.For<ISender>();
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, global::FoodDiary.Testing.RequestTestSender.Route((statistics, [typeof(global::FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics.ReadDashboardStatisticsQuery)]), (hydration, [typeof(global::FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery)])), new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(new GetDiaryStatisticsQuery(owner.Value), CancellationToken.None);

        Assert.Equal("Statistics.Unavailable", result.Error.Code);
        await hydration.DidNotReceive().Send(Arg.Is<ReadHydrationIntervalQuery>(q => true), Arg.Any<CancellationToken>());
    }

    private static UserDashboardProfileModel Profile(UserId owner) => new(owner.Value, Email: null, Language: "en", DashboardLayoutJson: null,
        DesiredWeightKg: null, DesiredWaistCm: null, HydrationGoal: 2000, WaterGoal: null, ProteinTarget: null, FatTarget: null,
        CarbTarget: null, FiberTarget: null, new UserCalorieSchedule(2000, CalorieCyclingEnabled: true, MondayCalories: null,
            TuesdayCalories: null, WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: 2500),
        "America/New_York");

    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    }
}
