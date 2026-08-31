using FluentValidation.TestHelper;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Statistics.Queries.GetStatistics;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public class DashboardValidatorTests {
    [Fact]
    public async Task GetDailyAdvice_WithNullUserId_HasError() {
        TestValidationResult<GetDailyAdviceQuery> result = await new GetDailyAdviceQueryValidator().TestValidateAsync(
            new GetDailyAdviceQuery(UserId: null, DateTime.UtcNow, "en"));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetStatistics_WithNullUserId_HasError() {
        TestValidationResult<GetStatisticsQuery> result = await new GetStatisticsQueryValidator().TestValidateAsync(
            new GetStatisticsQuery(UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
