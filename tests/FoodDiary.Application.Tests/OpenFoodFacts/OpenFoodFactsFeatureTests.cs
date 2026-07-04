using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;
using FoodDiary.Application.OpenFoodFacts.Services;

namespace FoodDiary.Application.Tests.OpenFoodFacts;

[ExcludeFromCodeCoverage]
public class OpenFoodFactsFeatureTests {
    private static OpenFoodFactsProductModel CreateProduct(string barcode = "4600000000001") =>
        new(barcode, "Test Product", "Brand", "Category", "https://example.com/img.jpg", 250, 10, 8, 30, 3);

    [Fact]
    public async Task SearchByBarcode_WhenProductFound_ReturnsProduct() {
        OpenFoodFactsProductModel product = CreateProduct();
        IOpenFoodFactsService service = CreateOpenFoodFactsService(product);
        var handler = new SearchByBarcodeQueryHandler(service);

        Result<OpenFoodFactsProductModel?> result = await handler.Handle(
            new SearchByBarcodeQuery("4600000000001"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal("4600000000001", result.Value.Barcode);
        Assert.Equal("Test Product", result.Value.Name);
        Assert.Equal("Brand", result.Value.Brand);
        Assert.Equal(250, result.Value.CaloriesPer100G);
    }

    [Fact]
    public async Task SearchByBarcode_WhenProductNotFound_ReturnsNull() {
        IOpenFoodFactsService service = CreateOpenFoodFactsService(barcodeResult: null);
        var handler = new SearchByBarcodeQueryHandler(service);

        Result<OpenFoodFactsProductModel?> result = await handler.Handle(
            new SearchByBarcodeQuery("0000000000000"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task SearchProducts_WhenResultsFound_ReturnsList() {
        var products = new List<OpenFoodFactsProductModel> {
            CreateProduct("111"),
            CreateProduct("222"),
        };
        IOpenFoodFactsService service = CreateOpenFoodFactsService(barcodeResult: null, products);
        IOpenFoodFactsProductCacheRepository cache = CreateOpenFoodFactsProductCacheRepository(
            out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var handler = new SearchOpenFoodFactsQueryHandler(CreateCachedProductSearch(service, cache, unitOfWork));

        Result<IReadOnlyList<OpenFoodFactsProductModel>> result = await handler.Handle(
            new SearchOpenFoodFactsQuery("test", 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("111", result.Value[0].Barcode);
        Assert.Equal("222", result.Value[1].Barcode);
        Assert.Equal(2, getUpsertedProducts().Count);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_WhenNoResults_ReturnsEmptyList() {
        IOpenFoodFactsService service = CreateOpenFoodFactsService(barcodeResult: null, []);
        var handler = new SearchOpenFoodFactsQueryHandler(CreateCachedProductSearch(service));

        Result<IReadOnlyList<OpenFoodFactsProductModel>> result = await handler.Handle(
            new SearchOpenFoodFactsQuery("nonexistent", 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchProducts_WhenCacheHasEnoughResults_DoesNotCallExternalSearch() {
        var cachedProducts = new List<OpenFoodFactsProductModel> {
            CreateProduct("cached-1"),
            CreateProduct("cached-2"),
        };
        IOpenFoodFactsService service = CreateOpenFoodFactsService(
            barcodeResult: null,
            [CreateProduct("external")],
            out Func<int> getSearchCallCount);
        var handler = new SearchOpenFoodFactsQueryHandler(
            CreateCachedProductSearch(service, CreateOpenFoodFactsProductCacheRepository(cachedProducts), CreateUnitOfWork()));

        Result<IReadOnlyList<OpenFoodFactsProductModel>> result = await handler.Handle(
            new SearchOpenFoodFactsQuery("test", 2),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("cached-1", result.Value[0].Barcode);
        Assert.Equal(0, getSearchCallCount());
    }

    private static IOpenFoodFactsService CreateOpenFoodFactsService(
        OpenFoodFactsProductModel? barcodeResult,
        IReadOnlyList<OpenFoodFactsProductModel>? searchResults = null) =>
        CreateOpenFoodFactsService(barcodeResult, searchResults, out _);

    private static IOpenFoodFactsService CreateOpenFoodFactsService(
        OpenFoodFactsProductModel? barcodeResult,
        IReadOnlyList<OpenFoodFactsProductModel>? searchResults,
        out Func<int> getSearchCallCount) {
        int searchCallCount = 0;
        IOpenFoodFactsService service = Substitute.For<IOpenFoodFactsService>();
        service
            .GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(barcodeResult));
        service
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                searchCallCount++;
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<OpenFoodFactsProductModel> result = (searchResults ?? [])
                    .Take(limit)
                    .ToList();
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
        IOpenFoodFactsProductCacheRepository? cache = null,
        IUnitOfWork? unitOfWork = null) {
        IOpenFoodFactsProductCacheRepository productCache = cache ?? CreateOpenFoodFactsProductCacheRepository();

        return new OpenFoodFactsCachedProductSearch(
            service,
            productCache,
            productCache,
            unitOfWork ?? CreateUnitOfWork());
    }

    private static IOpenFoodFactsProductCacheRepository CreateOpenFoodFactsProductCacheRepository(
        IReadOnlyList<OpenFoodFactsProductModel>? cachedProducts = null) =>
        CreateOpenFoodFactsProductCacheRepository(cachedProducts, out _);

    private static IOpenFoodFactsProductCacheRepository CreateOpenFoodFactsProductCacheRepository(
        out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts) =>
        CreateOpenFoodFactsProductCacheRepository(cachedProducts: null, out getUpsertedProducts);

    private static IOpenFoodFactsProductCacheRepository CreateOpenFoodFactsProductCacheRepository(
        IReadOnlyList<OpenFoodFactsProductModel>? cachedProducts,
        out Func<IReadOnlyList<OpenFoodFactsProductModel>> getUpsertedProducts) {
        IReadOnlyList<OpenFoodFactsProductModel> upsertedProducts = [];
        IOpenFoodFactsProductCacheRepository repository = Substitute.For<IOpenFoodFactsProductCacheRepository>();
        repository
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int limit = call.ArgAt<int>(1);
                IReadOnlyList<OpenFoodFactsProductModel> result = (cachedProducts ?? [])
                    .Take(limit)
                    .ToList();
                return Task.FromResult(result);
            });
        repository
            .UpsertAsync(Arg.Do<IReadOnlyCollection<OpenFoodFactsProductModel>>(products => upsertedProducts = products.ToList()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        getUpsertedProducts = () => upsertedProducts;
        return repository;
    }
}
