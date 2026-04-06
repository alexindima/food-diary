using FluentValidation.TestHelper;
using FoodDiary.Application.Lessons.Queries.GetLessons;

namespace FoodDiary.Application.Tests.Lessons;

public class LessonsValidatorTests {
    private readonly GetLessonsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError() {
        var query = new GetLessonsQuery(null, "en", null);
        var result = await _validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new GetLessonsQuery(Guid.NewGuid(), "en", null);
        var result = await _validator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
