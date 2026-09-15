using FluentValidation.TestHelper;
using FoodDiary.Modules.Tdee.Application.Queries.GetTdeeInsight;
using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;

namespace FoodDiary.Modules.Tdee.Application.Tests;

[ExcludeFromCodeCoverage]
public class TdeeValidatorTests {
    private readonly GetTdeeInsightQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError() {
        var query = new GetTdeeInsightQuery(UserId: null);
        TestValidationResult<GetTdeeInsightQuery> result = await _validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new GetTdeeInsightQuery(Guid.NewGuid());
        TestValidationResult<GetTdeeInsightQuery> result = await _validator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
