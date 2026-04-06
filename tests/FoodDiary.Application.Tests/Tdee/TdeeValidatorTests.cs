using FluentValidation.TestHelper;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

namespace FoodDiary.Application.Tests.Tdee;

public class TdeeValidatorTests {
    private readonly GetTdeeInsightQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError() {
        var query = new GetTdeeInsightQuery(null);
        var result = await _validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new GetTdeeInsightQuery(Guid.NewGuid());
        var result = await _validator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
