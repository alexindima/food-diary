using System.Globalization;
using FoodDiary.Mediator;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Modules.Dashboard.Application.Services;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories;
using FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DashboardCalendarTests {
    [Theory]
    [InlineData("unknown/zone", 2026, 9, 20)]
    [InlineData("UTC", 1, 1, 1)]
    [InlineData("UTC", 9999, 12, 31)]
    public async Task Context_WithInvalidCalendar_RejectsBeforeReadingUser(string zoneId, int year, int month, int day) {
        IDashboardUserContextService users = Substitute.For<IDashboardUserContextService>();
        var loader = new DashboardSectionDataLoader(Substitute.For<ISender>(), users, Substitute.For<IDashboardReadService>());
        var request = new DashboardSnapshotRequest(Guid.NewGuid(), new DateTime(year, month, day), DateTo: null, "en", 7, 1, 10, TimeZoneId: zoneId);

        Result<DashboardBuildContext> result = await loader.CreateBuildContextAsync(request, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(users.ReceivedCalls());
    }

    [Fact]
    public async Task Context_WithSkippedCalendarDay_UsesSystemTimeZoneHistory() {
        var date = new DateTime(2011, 12, 30);
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Apia");
        // Windows' mapped zone may omit Samoa's dateline change; Linux IANA data retains it.
        bool skipsDate = zone.GetUtcOffset(date.AddDays(1)) - zone.GetUtcOffset(date.AddDays(-1)) >= TimeSpan.FromDays(1);
        var user = User.Create("skipped-calendar@example.com", "hash");
        var request = new DashboardSnapshotRequest(user.Id.Value, date, DateTo: null, "en", 7, 1, 10,
            UserContext: new DashboardUserContextModel(user.Id.Value, user.Email, user.Language, user.DashboardLayoutJson,
                user.DesiredWeightKg, user.DesiredWaistCm, user.HydrationGoal, user.WaterGoal, user.ProteinTarget,
                user.FatTarget, user.CarbTarget, user.FiberTarget, default), TimeZoneId: zone.Id);
        var loader = new DashboardSectionDataLoader(Substitute.For<ISender>(), Substitute.For<IDashboardUserContextService>(), Substitute.For<IDashboardReadService>());

        Result<DashboardBuildContext> result = await loader.CreateBuildContextAsync(request, CancellationToken.None);

        if (skipsDate) {
            ResultAssert.Failure(result);
            Assert.Equal("Validation.Invalid", result.Error.Code);
        } else {
            DashboardBuildContext context = ResultAssert.Success(result);
            Assert.Equal(date, context.Calendar.Date);
            Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(date, zone), context.DayStart);
        }
    }

    public static IEnumerable<object[]> CalendarCases() {
        string[] zones = ["UTC", "Asia/Tbilisi", "America/Los_Angeles", "America/St_Johns", "Asia/Kathmandu", "Pacific/Kiritimati", "Pacific/Pago_Pago", "Europe/Berlin", "Australia/Lord_Howe"];
        string[] dates = ["2024-02-29", "2026-01-01", "2026-03-08", "2026-03-29", "2026-04-05", "2026-10-04", "2026-10-25", "2026-11-01", "2026-12-31"];
        foreach (string zone in zones) {
            foreach (string date in dates) {
                yield return [zone, date];
            }
        }
    }

    [Theory]
    [MemberData(nameof(CalendarCases))]
    public async Task Context_SeparatesCalendarDateFromInstant_AndForwardsDateToSections(string zoneId, string dateText) {
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var date = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var user = User.Create("dashboard-calendar@example.com", "hash");
        var request = new DashboardSnapshotRequest(user.Id.Value, date, DateTo: null, "en", 30, 1, 10,
            UserContext: new DashboardUserContextModel(user.Id.Value, user.Email, user.Language, user.DashboardLayoutJson,
                user.DesiredWeightKg, user.DesiredWaistCm, user.HydrationGoal, user.WaterGoal, user.ProteinTarget,
                user.FatTarget, user.CarbTarget, user.FiberTarget, default), TimeZoneId: zoneId);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetCurrentCycleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success<CycleModel?>(value: null));
        var loader = new DashboardSectionDataLoader(sender, Substitute.For<IDashboardUserContextService>(), Substitute.For<IDashboardReadService>());
        using var cancellation = new CancellationTokenSource();
        DashboardBuildContext context = ResultAssert.Success(await loader.CreateBuildContextAsync(request, cancellation.Token));
        Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone), context.DayStart);
        Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone).AddTicks(-1), context.DayEnd);
        Assert.Equal(date, context.Calendar.Date);
        Assert.Equal(date, context.Calendar.DateTo);
        Assert.Equal(date.AddDays(-29), context.Calendar.TrendDateFrom);
        Assert.Equal(1, context.PeriodDays);
        await loader.LoadAdviceAsync(context, cancellation.Token);
        await loader.LoadCaloriesBurnedAsync(context, cancellation.Token);
        await loader.LoadCycleAsync(request, context, cancellation.Token);
        await sender.Received(1).Send(Arg.Is<GetDailyAdviceQuery>(query => query.Date == date), cancellation.Token);
        await sender.Received(1).Send(Arg.Is<ReadExerciseCaloriesQuery>(query => query.DateUtc == date), cancellation.Token);
        await sender.Received(1).Send(Arg.Is<GetCurrentCycleQuery>(query => query.CurrentDate == day), cancellation.Token);
    }
}
