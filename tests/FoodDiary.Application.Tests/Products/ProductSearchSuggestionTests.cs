using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Queries.SearchProductSuggestions;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Presentation.Api.Features.Products.Mappings;

namespace FoodDiary.Application.Tests.Products;

public sealed class ProductSearchSuggestionTests {
    private readonly SearchProductSuggestionsQueryValidator _validator = new();

    [Fact]
    public async Task SearchProductSuggestionsQueryHandler_CallsAllProvidersAndCombinesResults() {
        var firstProvider = new StubProductSearchSuggestionProvider([
            new ProductSearchSuggestionModel(
                "openFoodFacts",
                "Fanta",
                "Coca-Cola",
                "Beverages",
                "5449000054227",
                null,
                "https://example.com/fanta.jpg",
                48,
                0,
                0,
                12,
                0),
        ]);
        var secondProvider = new StubProductSearchSuggestionProvider([
            new ProductSearchSuggestionModel(
                "usda",
                "FANTA, SODA, ORANGE",
                null,
                "Soda",
                null,
                539789,
                null,
                null,
                null,
                null,
                null,
                null),
        ]);
        var handler = new SearchProductSuggestionsQueryHandler([firstProvider, secondProvider]);

        var result = await handler.Handle(new SearchProductSuggestionsQuery("fanta", 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("openFoodFacts", result.Value[0].Source);
        Assert.Equal("usda", result.Value[1].Source);
        Assert.Equal(("fanta", 5), firstProvider.LastCall);
        Assert.Equal(("fanta", 5), secondProvider.LastCall);
    }

    [Fact]
    public async Task OpenFoodFactsProvider_WhenCacheHasEnoughResults_DoesNotCallExternalSearch() {
        var cachedProducts = new List<OpenFoodFactsProductModel> {
            CreateOpenFoodFactsProduct("cached-1"),
            CreateOpenFoodFactsProduct("cached-2"),
        };
        var service = new StubOpenFoodFactsService([CreateOpenFoodFactsProduct("external")]);
        var cache = new StubOpenFoodFactsProductCacheRepository(cachedProducts);
        var provider = new OpenFoodFactsProductSearchSuggestionProvider(service, cache);

        var result = await provider.SearchAsync("fanta", 2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, suggestion => Assert.Equal("openFoodFacts", suggestion.Source));
        Assert.Equal("cached-1", result[0].Barcode);
        Assert.Equal(0, service.SearchCallCount);
        Assert.Empty(cache.UpsertedProducts);
    }

    [Fact]
    public async Task OpenFoodFactsProvider_WhenCacheSparse_UpsertsExternalAndDeduplicatesByBarcode() {
        var service = new StubOpenFoodFactsService([
            CreateOpenFoodFactsProduct("external-1"),
            CreateOpenFoodFactsProduct("cached-1"),
        ]);
        var cache = new StubOpenFoodFactsProductCacheRepository([CreateOpenFoodFactsProduct("cached-1")]);
        var provider = new OpenFoodFactsProductSearchSuggestionProvider(service, cache);

        var result = await provider.SearchAsync("fanta", 5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("external-1", result[0].Barcode);
        Assert.Equal("cached-1", result[1].Barcode);
        Assert.Equal(2, cache.UpsertedProducts.Count);
        Assert.Equal(1, service.SearchCallCount);
    }

    [Fact]
    public async Task UsdaProvider_WhenLocalResultsSparse_AddsBrandedResultsWithoutDuplicates() {
        var repository = new StubUsdaFoodRepository([
            new UsdaFood {
                FdcId = 100,
                Description = "FANTA, SODA, ORANGE",
                FoodCategory = "Soda",
            },
        ]);
        var searchService = new StubUsdaFoodSearchService([
            new UsdaFoodModel(100, "Duplicate Fanta", "Soda"),
            new UsdaFoodModel(200, "FANTA ZERO, SODA, ORANGE", "Soda"),
        ]);
        var provider = new UsdaProductSearchSuggestionProvider(repository, searchService);

        var result = await provider.SearchAsync("fanta", 5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, suggestion => Assert.Equal("usda", suggestion.Source));
        Assert.Equal(100, result[0].UsdaFdcId);
        Assert.Equal(200, result[1].UsdaFdcId);
        Assert.Equal(("fanta", 4), searchService.LastBrandedSearchCall);
    }

    [Fact]
    public async Task UsdaProvider_WhenLocalResultsReachLimit_DoesNotCallBrandedSearch() {
        var repository = new StubUsdaFoodRepository([
            new UsdaFood {
                FdcId = 100,
                Description = "FANTA, SODA, ORANGE",
                FoodCategory = "Soda",
            },
        ]);
        var searchService = new StubUsdaFoodSearchService([]);
        var provider = new UsdaProductSearchSuggestionProvider(repository, searchService);

        var result = await provider.SearchAsync("fanta", 1, CancellationToken.None);

        Assert.Single(result);
        Assert.Null(searchService.LastBrandedSearchCall);
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithInvalidQuery_HasErrors() {
        var emptySearch = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("", 5));
        var tooLowLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 0));
        var tooHighLimit = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 21));

        emptySearch.ShouldHaveValidationErrorFor(q => q.Search);
        tooLowLimit.ShouldHaveValidationErrorFor(q => q.Limit);
        tooHighLimit.ShouldHaveValidationErrorFor(q => q.Limit);
    }

    [Fact]
    public async Task SearchProductSuggestionsValidator_WithValidQuery_HasNoErrors() {
        var result = await _validator.TestValidateAsync(new SearchProductSuggestionsQuery("fanta", 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProductSearchSuggestionHttpMapping_PreservesSourceSpecificFields() {
        IReadOnlyList<ProductSearchSuggestionModel> models = [
            new ProductSearchSuggestionModel(
                "openFoodFacts",
                "Fanta",
                "Coca-Cola",
                "Beverages",
                "5449000054227",
                null,
                "https://example.com/fanta.jpg",
                48,
                0,
                0,
                12,
                0),
            new ProductSearchSuggestionModel(
                "usda",
                "FANTA, SODA, ORANGE",
                null,
                "Soda",
                null,
                539789,
                null,
                null,
                null,
                null,
                null,
                null),
        ];

        var responses = models.ToHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal("openFoodFacts", responses[0].Source);
        Assert.Equal("5449000054227", responses[0].Barcode);
        Assert.Null(responses[0].UsdaFdcId);
        Assert.Equal("usda", responses[1].Source);
        Assert.Null(responses[1].Barcode);
        Assert.Equal(539789, responses[1].UsdaFdcId);
    }

    private static OpenFoodFactsProductModel CreateOpenFoodFactsProduct(string barcode) =>
        new(barcode, "Fanta", "Coca-Cola", "Beverages", "https://example.com/fanta.jpg", 48, 0, 0, 12, 0);

    private sealed class StubProductSearchSuggestionProvider(IReadOnlyList<ProductSearchSuggestionModel> suggestions)
        : IProductSearchSuggestionProvider {
        public string Source => "stub";
        public (string Search, int Limit)? LastCall { get; private set; }

        public Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
            string search,
            int limit,
            CancellationToken cancellationToken) {
            LastCall = (search, limit);
            return Task.FromResult(suggestions);
        }
    }

    private sealed class StubOpenFoodFactsService(IReadOnlyList<OpenFoodFactsProductModel> searchResults) : IOpenFoodFactsService {
        public int SearchCallCount { get; private set; }

        public Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OpenFoodFactsProductModel?>(null);

        public Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
            string query,
            int limit = 10,
            CancellationToken cancellationToken = default) {
            SearchCallCount++;
            return Task.FromResult(searchResults.Take(limit).ToList() as IReadOnlyList<OpenFoodFactsProductModel>);
        }
    }

    private sealed class StubOpenFoodFactsProductCacheRepository(
        IReadOnlyList<OpenFoodFactsProductModel>? cachedProducts = null) : IOpenFoodFactsProductCacheRepository {
        public IReadOnlyList<OpenFoodFactsProductModel> UpsertedProducts { get; private set; } = [];

        public Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
            string query,
            int limit = 10,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((cachedProducts ?? []).Take(limit).ToList() as IReadOnlyList<OpenFoodFactsProductModel>);

        public Task UpsertAsync(
            IReadOnlyCollection<OpenFoodFactsProductModel> products,
            CancellationToken cancellationToken = default) {
            UpsertedProducts = products.ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class StubUsdaFoodRepository(IReadOnlyList<UsdaFood> foods) : IUsdaFoodRepository {
        public Task<IReadOnlyList<UsdaFood>> SearchAsync(
            string query,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(foods.Take(limit).ToList() as IReadOnlyList<UsdaFood>);

        public Task<UsdaFood?> GetByFdcIdAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UsdaFood?>(null);

        public Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UsdaFoodNutrient>>([]);

        public Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UsdaFoodPortion>>([]);

        public Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
            IEnumerable<int> fdcIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>>(
                new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>());

        public Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
            string ageGroup = "adult",
            string gender = "all",
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, DailyReferenceValue>>(new Dictionary<int, DailyReferenceValue>());
    }

    private sealed class StubUsdaFoodSearchService(IReadOnlyList<UsdaFoodModel> brandedFoods) : IUsdaFoodSearchService {
        public (string Search, int Limit)? LastBrandedSearchCall { get; private set; }

        public Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
            string query,
            int limit = 20,
            CancellationToken cancellationToken = default) {
            LastBrandedSearchCall = (query, limit);
            return Task.FromResult(brandedFoods.Take(limit).ToList() as IReadOnlyList<UsdaFoodModel>);
        }

        public Task<UsdaFoodDetailModel?> GetFoodDetailAsync(
            int fdcId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UsdaFoodDetailModel?>(null);
    }
}
