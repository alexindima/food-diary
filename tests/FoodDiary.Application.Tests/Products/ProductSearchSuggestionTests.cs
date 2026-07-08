using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Queries.SearchProductSuggestions;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Domain.Entities.Usda;

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
        Assert.Equal(("fanta", 5), getSecondLastCall());
    }

    [Fact]
    public async Task OpenFoodFactsProvider_WhenCacheHasEnoughResults_DoesNotCallExternalSearch() {
        var cachedProducts = new List<OpenFoodFactsProductModel> {
            CreateOpenFoodFactsProduct("cached-1"),
            CreateOpenFoodFactsProduct("cached-2"),
        };
        IOpenFoodFactsService service = CreateOpenFoodFactsService(
            [CreateOpenFoodFactsProduct("external")],
            out Func<int> getSearchCallCount);
        IOpenFoodFactsProductCacheRepository cache = CreateOpenFoodFactsProductCacheRepository(
            cachedProducts,
            out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var provider = new OpenFoodFactsProductSearchSuggestionProvider(CreateCachedProductSearch(service, cache, unitOfWork));

        IReadOnlyList<ProductSearchSuggestionModel> result = await provider.SearchAsync("fanta", 2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, suggestion => Assert.Equal("openFoodFacts", suggestion.Source));
        Assert.Equal("cached-1", result[0].Barcode);
        Assert.Equal(0, getSearchCallCount());
        Assert.Empty(getUpsertedProducts());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenFoodFactsProvider_WhenCacheSparse_UpsertsExternalAndDeduplicatesByBarcode() {
        IOpenFoodFactsService service = CreateOpenFoodFactsService([
            CreateOpenFoodFactsProduct("external-1"),
            CreateOpenFoodFactsProduct("cached-1"),
        ], out Func<int> getSearchCallCount);
        IOpenFoodFactsProductCacheRepository cache = CreateOpenFoodFactsProductCacheRepository(
            [CreateOpenFoodFactsProduct("cached-1")],
            out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var provider = new OpenFoodFactsProductSearchSuggestionProvider(CreateCachedProductSearch(service, cache, unitOfWork));

        IReadOnlyList<ProductSearchSuggestionModel> result = await provider.SearchAsync("fanta", 5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("external-1", result[0].Barcode);
        Assert.Equal("cached-1", result[1].Barcode);
        Assert.Equal(2, getUpsertedProducts().Count);
        Assert.Equal(1, getSearchCallCount());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UsdaProvider_WhenLocalResultsSparse_AddsBrandedResultsWithoutDuplicates() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository([
            new UsdaFood {
                FdcId = 100,
                Description = "FANTA, SODA, ORANGE",
                FoodCategory = "Soda",
            },
        ]);
        IUsdaFoodSearchService searchService = CreateUsdaFoodSearchService([
            new UsdaFoodModel(100, "Duplicate Fanta", "Soda"),
            new UsdaFoodModel(200, "FANTA ZERO, SODA, ORANGE", "Soda"),
        ], out Func<(string Search, int Limit)?> getLastBrandedSearchCall);
        var provider = new UsdaProductSearchSuggestionProvider(repository, searchService);

        IReadOnlyList<ProductSearchSuggestionModel> result = await provider.SearchAsync("fanta", 5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, suggestion => Assert.Equal("usda", suggestion.Source));
        Assert.Equal(100, result[0].UsdaFdcId);
        Assert.Equal(200, result[1].UsdaFdcId);
        Assert.Equal(("fanta", 4), getLastBrandedSearchCall());
    }

    [Fact]
    public async Task UsdaProvider_WhenLocalResultsReachLimit_DoesNotCallBrandedSearch() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository([
            new UsdaFood {
                FdcId = 100,
                Description = "FANTA, SODA, ORANGE",
                FoodCategory = "Soda",
            },
        ]);
        IUsdaFoodSearchService searchService = CreateUsdaFoodSearchService(
            [],
            out Func<(string Search, int Limit)?> getLastBrandedSearchCall);
        var provider = new UsdaProductSearchSuggestionProvider(repository, searchService);

        IReadOnlyList<ProductSearchSuggestionModel> result = await provider.SearchAsync("fanta", 1, CancellationToken.None);

        Assert.Single(result);
        Assert.Null(getLastBrandedSearchCall());
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithInvalidQuery_HasErrors() {
        TestValidationResult<SearchProductSuggestionsQuery> emptySearch = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("", 5));
        TestValidationResult<SearchProductSuggestionsQuery> tooLowLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 0));
        TestValidationResult<SearchProductSuggestionsQuery> tooHighLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 21));

        emptySearch.ShouldHaveValidationErrorFor(q => q.Search);
        tooLowLimit.ShouldHaveValidationErrorFor(q => q.Limit);
        tooHighLimit.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithValidQuery_HasNoErrors() {
        TestValidationResult<SearchProductSuggestionsQuery> result = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static OpenFoodFactsProductModel CreateOpenFoodFactsProduct(string barcode) =>
        new(barcode, "Fanta", "Coca-Cola", "Beverages", "https://example.com/fanta.jpg", 48, 0, 0, 12, 0);

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

    private static IOpenFoodFactsService CreateOpenFoodFactsService(
        IReadOnlyList<OpenFoodFactsProductModel> searchResults,
        out Func<int> getSearchCallCount) {
        int searchCallCount = 0;
        IOpenFoodFactsService service = Substitute.For<IOpenFoodFactsService>();
        service
            .GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OpenFoodFactsProductModel?>(null));
        service
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                searchCallCount++;
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<OpenFoodFactsProductModel> result = searchResults.Take(limit).ToList();
                return Task.FromResult(result);
            });

        getSearchCallCount = () => searchCallCount;
        return service;
    }

    private static IUnitOfWork CreateUnitOfWork() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        return unitOfWork;
    }

    private static IOpenFoodFactsCachedProductSearch CreateCachedProductSearch(
        IOpenFoodFactsService service,
        IOpenFoodFactsProductCacheRepository cache,
        IUnitOfWork unitOfWork) =>
        new OpenFoodFactsCachedProductSearch(service, cache, cache, unitOfWork);

    private static IOpenFoodFactsProductCacheRepository CreateOpenFoodFactsProductCacheRepository(
        IReadOnlyList<OpenFoodFactsProductModel>? cachedProducts,
        out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts) {
        IReadOnlyList<OpenFoodFactsProductModel> upsertedProducts = [];
        IOpenFoodFactsProductCacheRepository repository = Substitute.For<IOpenFoodFactsProductCacheRepository>();
        repository
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<OpenFoodFactsProductModel> result = (cachedProducts ?? []).Take(limit).ToList();
                return Task.FromResult(result);
            });
        repository
            .UpsertAsync(Arg.Do<IReadOnlyCollection<OpenFoodFactsProductModel>>(products => upsertedProducts = products.ToList()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        getUpsertedProducts = () => upsertedProducts;
        return repository;
    }

    private static IUsdaFoodRepository CreateUsdaFoodRepository(IReadOnlyList<UsdaFood> foods) {
        IUsdaFoodRepository repository = Substitute.For<IUsdaFoodRepository>();
        repository
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<UsdaFood> result = foods.Take(limit).ToList();
                return Task.FromResult(result);
            });
        repository
            .SearchReadModelsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<UsdaFoodReadModel> result = foods
                    .Take(limit)
                    .Select(static food => new UsdaFoodReadModel(food.FdcId, food.Description, food.FoodCategory))
                    .ToList();
                return Task.FromResult(result);
            });
        repository
            .GetByFdcIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UsdaFood?>(null));
        repository
            .GetNutrientsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UsdaFoodNutrient>>([]));
        repository
            .GetPortionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UsdaFoodPortion>>([]));
        repository
            .GetNutrientsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>>(
                new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>()));
        repository
            .GetDailyReferenceValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, DailyReferenceValue>>(new Dictionary<int, DailyReferenceValue>()));
        return repository;
    }

    private static IUsdaFoodSearchService CreateUsdaFoodSearchService(
        IReadOnlyList<UsdaFoodModel> brandedFoods,
        out Func<(string Search, int Limit)?> getLastBrandedSearchCall) {
        (string Search, int Limit)? lastBrandedSearchCall = null;
        IUsdaFoodSearchService service = Substitute.For<IUsdaFoodSearchService>();
        service
            .SearchBrandedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string query = call.ArgAt<string>(0);
                int limit = call.ArgAt<int>(1);
                lastBrandedSearchCall = (query, limit);
                IReadOnlyList<UsdaFoodModel> result = brandedFoods.Take(limit).ToList();
                return Task.FromResult(result);
            });
        service
            .GetFoodDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UsdaFoodDetailModel?>(null));

        getLastBrandedSearchCall = () => lastBrandedSearchCall;
        return service;
    }
}
