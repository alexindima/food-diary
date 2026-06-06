using FluentValidation.TestHelper;
using FoodDiary.Application.Recipes.Queries.ExploreRecipes;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class ExploreRecipesQueryValidatorTests {
    private readonly ExploreRecipesQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithPageZero_HasError() {
        var query = new ExploreRecipesQuery(Guid.NewGuid(), 0, 10, null, null, null, "newest");
        TestValidationResult<ExploreRecipesQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Fact]
    public async Task Validate_WithLimitZero_HasError() {
        var query = new ExploreRecipesQuery(Guid.NewGuid(), 1, 0, null, null, null, "newest");
        TestValidationResult<ExploreRecipesQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task Validate_WithLimitOver50_HasError() {
        var query = new ExploreRecipesQuery(Guid.NewGuid(), 1, 51, null, null, null, "newest");
        TestValidationResult<ExploreRecipesQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task Validate_WithInvalidSortBy_HasError() {
        var query = new ExploreRecipesQuery(Guid.NewGuid(), 1, 10, null, null, null, "invalid");
        TestValidationResult<ExploreRecipesQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.SortBy);
    }

    [Theory]
    [InlineData("newest")]
    [InlineData("popular")]
    public async Task Validate_WithValidSortBy_NoErrors(string sortBy) {
        var query = new ExploreRecipesQuery(Guid.NewGuid(), 1, 10, null, null, null, sortBy);
        TestValidationResult<ExploreRecipesQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
