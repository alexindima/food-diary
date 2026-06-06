using FluentValidation.TestHelper;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

namespace FoodDiary.Application.Tests.Usda;

[ExcludeFromCodeCoverage]
public class UsdaValidatorTests {
    private readonly SearchUsdaFoodsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptySearch_HasError() {
        var query = new SearchUsdaFoodsQuery("");
        TestValidationResult<SearchUsdaFoodsQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Search);
    }

    [Fact]
    public async Task Validate_WithLimitTooLow_HasError() {
        var query = new SearchUsdaFoodsQuery("chicken", Limit: 0);
        TestValidationResult<SearchUsdaFoodsQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task Validate_WithLimitTooHigh_HasError() {
        var query = new SearchUsdaFoodsQuery("chicken", Limit: 101);
        TestValidationResult<SearchUsdaFoodsQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new SearchUsdaFoodsQuery("chicken", Limit: 20);
        TestValidationResult<SearchUsdaFoodsQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
