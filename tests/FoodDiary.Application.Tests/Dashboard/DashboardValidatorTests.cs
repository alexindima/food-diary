using FluentValidation.TestHelper;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Statistics.Queries.GetStatistics;

namespace FoodDiary.Application.Tests.Dashboard;

public class DashboardValidatorTests {
    [Fact]
    public async Task GetDashboardSnapshot_WithNullUserId_HasError() {
        var result = await new GetDashboardSnapshotQueryValidator().TestValidateAsync(
            new GetDashboardSnapshotQuery(null, DateTime.UtcNow, 1, 10, "en", 7));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetDailyAdvice_WithNullUserId_HasError() {
        var result = await new GetDailyAdviceQueryValidator().TestValidateAsync(
            new GetDailyAdviceQuery(null, DateTime.UtcNow, "en"));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetStatistics_WithNullUserId_HasError() {
        var result = await new GetStatisticsQueryValidator().TestValidateAsync(
            new GetStatisticsQuery(null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
