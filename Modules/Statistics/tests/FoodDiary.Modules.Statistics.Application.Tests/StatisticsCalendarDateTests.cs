using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsCalendarDateTests {
    public static IEnumerable<object[]> CalendarCases() {
        string[] zones = ["UTC", "Asia/Tbilisi", "America/Los_Angeles", "America/St_Johns", "Asia/Kathmandu", "Pacific/Kiritimati", "Pacific/Pago_Pago", "Europe/Berlin", "Australia/Lord_Howe"];
        string[] days = ["2024-02-29", "2026-01-01", "2026-03-08", "2026-03-29", "2026-09-21", "2026-10-04", "2026-10-25", "2026-11-01", "2026-12-31"];
        foreach (string zone in zones) {
            foreach (string day in days) {
                yield return [zone, day];
            }
        }
    }

    [Theory]
    [MemberData(nameof(CalendarCases))]
    public async Task Summary_UsesExactCalendarDayForMeasurements_AndLocalDayInstantsForMeals(string zoneId, string dateText) {
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        DateTime from = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
        DateTime to = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone).AddTicks(-1);
        var calendarDate = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var userId = new UserId(Guid.NewGuid());
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReadMealNutritionStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealNutritionStatisticsBucket>>([])));
        sender.Send(Arg.Any<ReadWeightSummariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntrySummaryModel>>([new(calendarDate, calendarDate, 75)]));
        sender.Send(Arg.Any<ReadWaistSummariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WaistEntrySummaryModel>>([new(calendarDate, calendarDate, 82)]));
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Error?>(null));
        using var cancellation = new CancellationTokenSource();
        var query = new GetStatisticsSummaryQuery(userId.Value, from, to, 1, day, day);

        Result<StatisticsSummaryModel> result = await new GetStatisticsSummaryQueryHandler(sender, access).Handle(query, cancellation.Token);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(75, Assert.Single(result.Value.Weight).AverageWeightKg),
            () => Assert.Equal(82, Assert.Single(result.Value.Waist).AverageCircumferenceCm));
        await sender.Received(1).Send(Arg.Is<ReadWeightSummariesQuery>(q => q.UserId == userId && q.DateFrom == calendarDate && q.DateTo == calendarDate), cancellation.Token);
        await sender.Received(1).Send(Arg.Is<ReadWaistSummariesQuery>(q => q.UserId == userId && q.DateFrom == calendarDate && q.DateTo == calendarDate), cancellation.Token);
        await sender.Received(1).Send(Arg.Is<ReadMealNutritionStatisticsQuery>(q => q.UserId == userId && q.DateFrom == from && q.DateTo == to), cancellation.Token);
        Assert.True((await new GetStatisticsSummaryQueryValidator().ValidateAsync(query)).IsValid);
    }

    [Theory]
    [InlineData("2026-09-21", null)]
    [InlineData(null, "2026-09-21")]
    [InlineData("2026-09-22", "2026-09-21")]
    [InlineData("2024-01-01", "2026-01-01")]
    public async Task Summary_RejectsIncompleteInvertedAndExcessiveBodyRanges(string? fromText, string? toText) {
        DateOnly? from = fromText is null ? null : DateOnly.ParseExact(fromText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        DateOnly? to = toText is null ? null : DateOnly.ParseExact(toText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var query = new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.Date, DateTime.UtcNow.Date, 1, from, to);
        ISender sender = Substitute.For<ISender>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Error?>(null));
        Result<StatisticsSummaryModel> result = await new GetStatisticsSummaryQueryHandler(sender, access).Handle(query, CancellationToken.None);
        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Empty(sender.ReceivedCalls());
        Assert.False((await new GetStatisticsSummaryQueryValidator().ValidateAsync(query)).IsValid);
    }
}
