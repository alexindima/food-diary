using FoodDiary.Modules.Meals.Application.Queries.ReadMealNutritionStatistics;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReadMealNutritionStatisticsQueryHandlerTests {
    [Theory]
    [InlineData(null)]
    [InlineData("Asia/Tbilisi")]
    [InlineData("America/Los_Angeles")]
    [InlineData("Australia/Lord_Howe")]
    public async Task Handle_ForwardsCalendarZoneAndCancellation_KeepingLegacyInstantMode(string? zoneId) {
        IMealNutritionStatisticsReadService reader = Substitute.For<IMealNutritionStatisticsReadService>();
        var user = UserId.New();
        var from = new DateTime(2026, 10, 3, 20, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(1).AddTicks(-1);
        using var cancellation = new CancellationTokenSource();
        reader.GetStatisticsAsync(user, from, to, 1, cancellation.Token, Arg.Any<TimeZoneInfo?>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealNutritionStatisticsBucket>>([])));

        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await new ReadMealNutritionStatisticsQueryHandler(reader)
            .Handle(new ReadMealNutritionStatisticsQuery(user, from, to, 1, zoneId), cancellation.Token);

        Assert.True(result.IsSuccess);
        await reader.Received(1).GetStatisticsAsync(user, from, to, 1, cancellation.Token,
            Arg.Is<TimeZoneInfo?>(zone => zoneId == null ? zone == null : zone != null && zone.Id == zoneId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Not/A_TimeZone")]
    public async Task Handle_RejectsInvalidTimeZoneBeforeReadingMeals(string zoneId) {
        IMealNutritionStatisticsReadService reader = Substitute.For<IMealNutritionStatisticsReadService>();
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await new ReadMealNutritionStatisticsQueryHandler(reader)
            .Handle(new ReadMealNutritionStatisticsQuery(UserId.New(), DateTime.UtcNow.Date, DateTime.UtcNow.Date, 1, zoneId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(reader.ReceivedCalls());
    }
}
