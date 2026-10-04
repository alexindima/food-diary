using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dashboard.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FoodDiary.Infrastructure.Tests.Persistence.Dashboard;

[ExcludeFromCodeCoverage]
public sealed class DashboardStatisticsReadServiceTests {
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(366)]
    public async Task GetStatisticsAsync_MatchesReferenceAggregationForUnorderedBoundaryMeals(int quantizationDays) {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"bucket-boundaries-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        var from = new DateTime(2025, 1, 1, 13, 17, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(365).AddHours(1);
        DateTime[] dates = [to, from.AddTicks(-1), from.AddDays(7), from, from.AddDays(1).AddTicks(-1),
            from.AddDays(1), to.AddTicks(1), from.AddDays(100), from.AddDays(7).AddTicks(-1)];
        Meal[] meals = [.. dates.Select((date, index) => CreateMeal(user.Id, date, 100 + (index * 0.17), 10 + (index * 0.13), index == 7 ? null : (MealType)(index % 5)))];
        context.Meals.AddRange(meals);
        context.Meals.Add(CreateMeal(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId.New(), from, 9999, 9999));
        await context.SaveChangesAsync();
        var service = new MealNutritionStatisticsReadService(context.Meals);

        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await service.GetStatisticsAsync(user.Id, from, to, quantizationDays);

        Assert.True(result.IsSuccess, result.Error.Message);
        IReadOnlyList<(DateTime Start, DateTime End)> ranges = FoodDiary.Application.Contracts.Common.Validation.TemporalRangePolicy.BuildInstantBuckets(from, to, quantizationDays);
        Assert.Equal(ranges.Count, result.Value.Count);
        for (int index = 0; index < ranges.Count; index++) {
            (DateTime start, DateTime end) = ranges[index];
            Meal[] expected = [.. meals.Where(meal => meal.Date >= start && meal.Date <= end)];
            MealNutritionStatisticsBucket actual = result.Value[index];
            int days = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));
            Assert.Equal(start, actual.DateFrom);
            Assert.Equal(end, actual.DateTo);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.TotalCalories);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalProteins), 2, MidpointRounding.ToEven), actual.TotalProteins);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalProteins) / days, 2, MidpointRounding.ToEven), actual.AverageProteins);
            Assert.Equal(expected.Sum(meal => meal.TotalFats), actual.TotalFats);
            Assert.Equal(expected.Sum(meal => meal.TotalCarbs), actual.TotalCarbs);
            Assert.Equal(expected.Sum(meal => meal.TotalFiber), actual.TotalFiber);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalFats) / days, 2, MidpointRounding.ToEven), actual.AverageFats);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalCarbs) / days, 2, MidpointRounding.ToEven), actual.AverageCarbs);
            Assert.Equal(Math.Round(expected.Sum(meal => meal.TotalFiber) / days, 2, MidpointRounding.ToEven), actual.AverageFiber);
            Assert.Equal(Math.Round(expected.Where(meal => meal.MealType == MealType.Breakfast).Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.BreakfastCalories);
            Assert.Equal(Math.Round(expected.Where(meal => meal.MealType == MealType.Lunch).Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.LunchCalories);
            Assert.Equal(Math.Round(expected.Where(meal => meal.MealType == MealType.Dinner).Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.DinnerCalories);
            Assert.Equal(Math.Round(expected.Where(meal => meal.MealType == MealType.Snack).Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.SnackCalories);
            Assert.Equal(Math.Round(expected.Where(meal => meal.MealType is null or MealType.Other).Sum(meal => meal.TotalCalories), 2, MidpointRounding.ToEven), actual.OtherCalories);
            Assert.Equal(expected.Length, actual.MealCount);
            Assert.Equal(expected.Select(meal => meal.Date.Date).Distinct().Count(), actual.TrackedDayCount);
        }
    }

    [Fact]
    public async Task GetStatisticsAsync_ProjectsMealNutritionIntoBuckets() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-statistics-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);

        Meal firstMeal = CreateMeal(user.Id, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc), calories: 500, proteins: 30);
        Meal secondMeal = CreateMeal(user.Id, new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc), calories: 700, proteins: 40);
        Meal thirdMeal = CreateMeal(user.Id, new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc), calories: 300, proteins: 15);
        context.Meals.AddRange(firstMeal, secondMeal, thirdMeal);
        await context.SaveChangesAsync();

        var readService = new DashboardStatisticsReadService(new MealNutritionStatisticsReadService(context.Meals));

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            user.Id,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 2, 23, 59, 59, DateTimeKind.Utc),
            quantizationDays: 1,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Collection(
            result.Value,
            first => {
                Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), first.DateFrom);
                Assert.Equal(1200, first.TotalCalories);
                Assert.Equal(70, first.AverageProteins);
                Assert.Equal(70, first.TotalProteins);
            },
            second => {
                Assert.Equal(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), second.DateFrom);
                Assert.Equal(300, second.TotalCalories);
                Assert.Equal(15, second.AverageProteins);
                Assert.Equal(15, second.TotalProteins);
            });
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenDateRangeIsInvalid_ReturnsValidationFailure() {
        await using FoodDiaryDbContext context = CreateContext();
        var readService = new DashboardStatisticsReadService(new MealNutritionStatisticsReadService(context.Meals));

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId.New(),
            new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            quantizationDays: 1,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsAsync_AtMaximumInstant_ReturnsSingleBucketWithoutOverflow() {
        await using FoodDiaryDbContext context = CreateContext();
        var readService = new DashboardStatisticsReadService(new MealNutritionStatisticsReadService(context.Meals));

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId.New(),
            DateTime.MaxValue,
            DateTime.MaxValue,
            quantizationDays: 1,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        DashboardStatisticsBucketReadModel bucket = Assert.Single(result.Value);
        Assert.Multiple(
            () => Assert.Equal(DateTime.MaxValue, bucket.DateFrom),
            () => Assert.Equal(DateTime.MaxValue, bucket.DateTo));
    }

    [Theory]
    [InlineData(366, 1)]
    [InlineData(1, 367)]
    [InlineData(1, int.MaxValue)]
    public async Task GetStatisticsAsync_WithUnsupportedTemporalRange_ReturnsValidationFailure(
        int periodDays,
        int quantizationDays) {
        await using FoodDiaryDbContext context = CreateContext();
        var readService = new DashboardStatisticsReadService(new MealNutritionStatisticsReadService(context.Meals));
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId.New(),
            from,
            from.AddDays(periodDays),
            quantizationDays,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void NormalizeUtcInstant_HandlesAllDateTimeKinds(DateTimeKind kind) {
        DateTime value = new(2026, 6, 1, 12, 0, 0, kind);

        DateTime normalized = InvokeNormalizeUtcInstant(value);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
    }

    [Theory]
    [InlineData("Asia/Tbilisi")]
    [InlineData("America/Los_Angeles")]
    [InlineData("Pacific/Kiritimati")]
    public async Task CalendarRangeLimit_CountsLocalDatesInsteadOfTouchedUtcDates(string zoneId) {
        await using FoodDiaryDbContext context = CreateContext();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var start = new DateTime(2024, 1, 1);
        DateTime from = TimeZoneInfo.ConvertTimeToUtc(start, zone);
        DateTime to = TimeZoneInfo.ConvertTimeToUtc(start.AddDays(366), zone).AddTicks(-1);
        var service = new MealNutritionStatisticsReadService(context.Meals);
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await service.GetStatisticsAsync(
            FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId.New(), from, to, 1, CancellationToken.None, zone);
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(366, result.Value.Count);
    }

    [Theory]
    [InlineData("Asia/Tbilisi", "2026-10-04", 24)]
    [InlineData("America/Los_Angeles", "2026-03-08", 23)]
    [InlineData("America/Los_Angeles", "2026-11-01", 25)]
    [InlineData("Australia/Lord_Howe", "2026-10-04", 23.5)]
    [InlineData("Australia/Lord_Howe", "2026-04-05", 24.5)]
    public async Task CalendarBuckets_CountLocalMealDatesAndKeepVariableLengthDays(string zoneId, string dateText, double hours) {
        await using FoodDiaryDbContext context = CreateContext();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        DateTime from = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
        DateTime nextDay = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        DateTime to = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(2).ToDateTime(TimeOnly.MinValue), zone).AddTicks(-1);
        Assert.Equal(hours, (nextDay - from).TotalHours);
        var user = User.Create($"calendar-statistics-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.Meals.AddRange(CreateMeal(user.Id, from.AddMinutes(30), 100, 10),
            CreateMeal(user.Id, nextDay.AddMinutes(-30), 200, 20), CreateMeal(user.Id, nextDay, 400, 40));
        await context.SaveChangesAsync();
        var service = new MealNutritionStatisticsReadService(context.Meals);

        Result<IReadOnlyList<MealNutritionStatisticsBucket>> daily = await service.GetStatisticsAsync(user.Id, from, to, 1, CancellationToken.None, zone);
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> combined = await service.GetStatisticsAsync(user.Id, from, to, 2, CancellationToken.None, zone);

        Assert.True(daily.IsSuccess, daily.Error.Message);
        Assert.True(combined.IsSuccess, combined.Error.Message);
        Assert.Collection(daily.Value,
            first => {
                Assert.Equal(from, first.DateFrom);
                Assert.Equal(nextDay.AddTicks(-1), first.DateTo);
                Assert.Equal(300, first.TotalCalories);
                Assert.Equal(30, first.AverageProteins);
                Assert.Equal(2, first.MealCount);
                Assert.Equal(1, first.TrackedDayCount);
            },
            second => {
                Assert.Equal(nextDay, second.DateFrom);
                Assert.Equal(to, second.DateTo);
                Assert.Equal(400, second.TotalCalories);
                Assert.Equal(1, second.TrackedDayCount);
            });
        MealNutritionStatisticsBucket bucket = Assert.Single(combined.Value);
        Assert.Equal(700, bucket.TotalCalories);
        Assert.Equal(35, bucket.AverageProteins);
        Assert.Equal(3, bucket.MealCount);
        Assert.Equal(2, bucket.TrackedDayCount);
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static DateTime InvokeNormalizeUtcInstant(DateTime value) {
        MethodInfo method = typeof(MealNutritionStatisticsReadService).GetMethod(
            "NormalizeUtcInstant",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (DateTime)method.Invoke(null, [value])!;
    }

    private static Meal CreateMeal(
        FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId userId,
        DateTime date,
        double calories,
        double proteins,
        MealType? mealType = null) {
        var meal = Meal.Create(userId, date, mealType);
        meal.ApplyNutrition(new MealNutritionUpdate(
            calories,
            proteins,
            TotalFats: 10,
            TotalCarbs: 20,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: calories,
            ManualProteins: proteins,
            ManualFats: 10,
            ManualCarbs: 20,
            ManualFiber: 5,
            ManualAlcohol: 0));
        return meal;
    }
}
