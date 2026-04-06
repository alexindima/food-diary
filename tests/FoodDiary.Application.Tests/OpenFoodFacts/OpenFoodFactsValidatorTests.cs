using FluentValidation.TestHelper;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

namespace FoodDiary.Application.Tests.OpenFoodFacts;

public class OpenFoodFactsValidatorTests {
    private readonly SearchByBarcodeQueryValidator _barcodeValidator = new();
    private readonly SearchOpenFoodFactsQueryValidator _searchValidator = new();

    [Fact]
    public async Task BarcodeValidator_WithEmptyBarcode_HasError() {
        var query = new SearchByBarcodeQuery("");
        var result = await _barcodeValidator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Barcode);
    }

    [Fact]
    public async Task BarcodeValidator_WithTooLongBarcode_HasError() {
        var query = new SearchByBarcodeQuery(new string('1', 129));
        var result = await _barcodeValidator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Barcode);
    }

    [Fact]
    public async Task BarcodeValidator_WithValidBarcode_NoErrors() {
        var query = new SearchByBarcodeQuery("4600000000001");
        var result = await _barcodeValidator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task BarcodeValidator_WithMaxLengthBarcode_NoErrors() {
        var query = new SearchByBarcodeQuery(new string('1', 128));
        var result = await _barcodeValidator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task SearchValidator_WithEmptySearch_HasError() {
        var query = new SearchOpenFoodFactsQuery("");
        var result = await _searchValidator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Search);
    }

    [Fact]
    public async Task SearchValidator_WithLimitTooLow_HasError() {
        var query = new SearchOpenFoodFactsQuery("milk", Limit: 0);
        var result = await _searchValidator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task SearchValidator_WithLimitTooHigh_HasError() {
        var query = new SearchOpenFoodFactsQuery("milk", Limit: 51);
        var result = await _searchValidator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task SearchValidator_WithValidQuery_NoErrors() {
        var query = new SearchOpenFoodFactsQuery("milk", Limit: 10);
        var result = await _searchValidator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
