using FluentValidation.TestHelper;
using FoodDiary.Application.Statistics.Queries.GetStatistics;

namespace FoodDiary.Application.Tests.Queries;

[ExcludeFromCodeCoverage]
public sealed class GetStatisticsQueryValidatorTests {
    [Fact]
    public async Task GetStatistics_WithNullUserId_HasError() {
        TestValidationResult<GetStatisticsQuery> result = await new GetStatisticsQueryValidator().TestValidateAsync(
            new GetStatisticsQuery(UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1));
        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }
}
