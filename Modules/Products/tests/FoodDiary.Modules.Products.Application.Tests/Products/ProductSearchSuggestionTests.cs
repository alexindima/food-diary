using FluentValidation.TestHelper;
using FoodDiary.Results;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Queries.SearchProductSuggestions;

namespace FoodDiary.Application.Tests.Products;

[ExcludeFromCodeCoverage]
public sealed class ProductSearchSuggestionTests {
    private readonly SearchProductSuggestionsQueryValidator _validator = new();

    [Fact]
    public async Task SearchProductSuggestionsQueryHandler_CallsAllProvidersAndCombinesResults() {
        IProductSearchSuggestionProvider firstProvider = CreateProductSearchSuggestionProvider([
            new ProductSearchSuggestionModel(
                "openFoodFacts",
                "Fanta",
                "Coca-Cola",
                "Beverages",
                "5449000054227",
                UsdaFdcId: null,
                "https://example.com/fanta.jpg",
                48,
                0,
                0,
                12,
                0),
        ], out Func<(string Search, int Limit)?> getFirstLastCall);
        IProductSearchSuggestionProvider secondProvider = CreateProductSearchSuggestionProvider([
            new ProductSearchSuggestionModel(
                "usda",
                "FANTA, SODA, ORANGE",
                Brand: null,
                "Soda",
                Barcode: null,
                539789,
                ImageUrl: null,
                CaloriesPer100G: null,
                ProteinsPer100G: null,
                FatsPer100G: null,
                CarbsPer100G: null,
                FiberPer100G: null),
        ], out Func<(string Search, int Limit)?> getSecondLastCall);
        var handler = new SearchProductSuggestionsQueryHandler([firstProvider, secondProvider]);

        Result<IReadOnlyList<ProductSearchSuggestionModel>> result = await handler.Handle(new SearchProductSuggestionsQuery("fanta", 5), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("openFoodFacts", result.Value[0].Source);
        Assert.Equal("usda", result.Value[1].Source);
        Assert.Equal(("fanta", 5), getFirstLastCall());
        Assert.Equal(("fanta", 4), getSecondLastCall());
    }

    [Fact]
    public async Task SearchProductSuggestionsQueryHandler_WhenProviderFillsBudget_SkipsRemainingProviders() {
        IProductSearchSuggestionProvider firstProvider = CreateProductSearchSuggestionProvider([
            CreateSuggestion("first"),
            CreateSuggestion("second"),
            CreateSuggestion("overflow"),
        ], out _);
        IProductSearchSuggestionProvider secondProvider = CreateProductSearchSuggestionProvider(
            [CreateSuggestion("unused")],
            out Func<(string Search, int Limit)?> getSecondLastCall);
        var handler = new SearchProductSuggestionsQueryHandler([firstProvider, secondProvider]);

        Result<IReadOnlyList<ProductSearchSuggestionModel>> result = await handler.Handle(
            new SearchProductSuggestionsQuery("fanta", 2),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(2, result.Value.Count),
            () => Assert.Null(getSecondLastCall()));
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithInvalidQuery_HasErrors() {
        TestValidationResult<SearchProductSuggestionsQuery> emptySearch = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("", 5));
        TestValidationResult<SearchProductSuggestionsQuery> tooLowLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 0));
        TestValidationResult<SearchProductSuggestionsQuery> tooHighLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 21));
        TestValidationResult<SearchProductSuggestionsQuery> tooLongSearch = await _validator.TestValidateAsync(
            new SearchProductSuggestionsQuery(new string('x', SearchProductSuggestionsQueryValidator.MaximumSearchLength + 1), 5));

        emptySearch.ShouldHaveValidationErrorFor(q => q.Search);
        tooLongSearch.ShouldHaveValidationErrorFor(q => q.Search);
        tooLowLimit.ShouldHaveValidationErrorFor(q => q.Limit);
        tooHighLimit.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithValidQuery_HasNoErrors() {
        TestValidationResult<SearchProductSuggestionsQuery> result = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static IProductSearchSuggestionProvider CreateProductSearchSuggestionProvider(
        IReadOnlyList<ProductSearchSuggestionModel> suggestions,
        out Func<(string Search, int Limit)?> getLastCall) {
        (string Search, int Limit)? lastCall = null;
        IProductSearchSuggestionProvider provider = Substitute.For<IProductSearchSuggestionProvider>();
        provider.Source.Returns("stub");
        provider
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                lastCall = (call.ArgAt<string>(0), call.ArgAt<int>(1));
                return Task.FromResult(suggestions);
            });

        getLastCall = () => lastCall;
        return provider;
    }

    private static ProductSearchSuggestionModel CreateSuggestion(string name) =>
        new(
            "test",
            name,
            Brand: null,
            Category: null,
            Barcode: null,
            UsdaFdcId: null,
            ImageUrl: null,
            CaloriesPer100G: null,
            ProteinsPer100G: null,
            FatsPer100G: null,
            CarbsPer100G: null,
            FiberPer100G: null);
}
