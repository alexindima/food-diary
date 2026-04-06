using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

namespace FoodDiary.Application.Tests.OpenFoodFacts;

public class OpenFoodFactsFeatureTests {
    private static OpenFoodFactsProductModel CreateProduct(string barcode = "4600000000001") =>
        new(barcode, "Test Product", "Brand", "Category", "https://example.com/img.jpg", 250, 10, 8, 30, 3);

    [Fact]
    public async Task SearchByBarcode_WhenProductFound_ReturnsProduct() {
        var product = CreateProduct();
        var service = new StubOpenFoodFactsService(product);
        var handler = new SearchByBarcodeQueryHandler(service);

        var result = await handler.Handle(
            new SearchByBarcodeQuery("4600000000001"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("4600000000001", result.Value.Barcode);
        Assert.Equal("Test Product", result.Value.Name);
        Assert.Equal("Brand", result.Value.Brand);
        Assert.Equal(250, result.Value.CaloriesPer100G);
    }

    [Fact]
    public async Task SearchByBarcode_WhenProductNotFound_ReturnsNull() {
        var service = new StubOpenFoodFactsService(null);
        var handler = new SearchByBarcodeQueryHandler(service);

        var result = await handler.Handle(
            new SearchByBarcodeQuery("0000000000000"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task SearchProducts_WhenResultsFound_ReturnsList() {
        var products = new List<OpenFoodFactsProductModel> {
            CreateProduct("111"),
            CreateProduct("222"),
        };
        var service = new StubOpenFoodFactsService(null, products);
        var handler = new SearchOpenFoodFactsQueryHandler(service);

        var result = await handler.Handle(
            new SearchOpenFoodFactsQuery("test", 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("111", result.Value[0].Barcode);
        Assert.Equal("222", result.Value[1].Barcode);
    }

    [Fact]
    public async Task SearchProducts_WhenNoResults_ReturnsEmptyList() {
        var service = new StubOpenFoodFactsService(null, []);
        var handler = new SearchOpenFoodFactsQueryHandler(service);

        var result = await handler.Handle(
            new SearchOpenFoodFactsQuery("nonexistent", 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private sealed class StubOpenFoodFactsService(
        OpenFoodFactsProductModel? barcodeResult,
        IReadOnlyList<OpenFoodFactsProductModel>? searchResults = null) : IOpenFoodFactsService {
        public Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
            string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult(barcodeResult);

        public Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
            string query, int limit = 10, CancellationToken cancellationToken = default) =>
            Task.FromResult(searchResults ?? (IReadOnlyList<OpenFoodFactsProductModel>)[]);
    }
}
