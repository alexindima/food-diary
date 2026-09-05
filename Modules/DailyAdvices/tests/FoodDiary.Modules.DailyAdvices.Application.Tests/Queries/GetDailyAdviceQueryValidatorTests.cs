using FluentValidation.TestHelper;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

namespace FoodDiary.Application.Tests.Queries;

[ExcludeFromCodeCoverage]
public sealed class GetDailyAdviceQueryValidatorTests {
    [Fact]
    public async Task GetDailyAdvice_WithNullUserId_HasError() {
        TestValidationResult<GetDailyAdviceQuery> result = await new GetDailyAdviceQueryValidator().TestValidateAsync(
            new GetDailyAdviceQuery(UserId: null, DateTime.UtcNow, "en"));
        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }
}
