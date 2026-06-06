using System.Net;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsServiceTests {
    [Fact]
    public async Task GetByBarcodeAsync_WhenProductFound_ReturnsMappedProduct() {
        string json = """
            {
              "status": 1,
              "product": {
                "product_name": "ÐœÐ¾Ð»Ð¾ÐºÐ¾ 3.2%",
                "brands": "ÐŸÑ€Ð¾ÑÑ‚Ð¾ÐºÐ²Ð°ÑˆÐ¸Ð½Ð¾",
                "categories": "Dairy",
                "image_url": "https://images.openfoodfacts.org/test.jpg",
                "nutriments": {
                  "energy-kcal_100g": 60,
                  "proteins_100g": 3.2,
                  "fat_100g": 3.2,
                  "carbohydrates_100g": 4.7,
                  "fiber_100g": 0
                }
              }
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("4600000000001");

        Assert.NotNull(result);
        Assert.Equal("4600000000001", result.Barcode);
        Assert.Equal("ÐœÐ¾Ð»Ð¾ÐºÐ¾ 3.2%", result.Name);
        Assert.Equal("ÐŸÑ€Ð¾ÑÑ‚Ð¾ÐºÐ²Ð°ÑˆÐ¸Ð½Ð¾", result.Brand);
        Assert.Equal("Dairy", result.Category);
        Assert.Equal(60, result.CaloriesPer100G);
        Assert.Equal(3.2, result.ProteinsPer100G);
        Assert.Equal(3.2, result.FatsPer100G);
        Assert.Equal(4.7, result.CarbsPer100G);
        Assert.Equal(0, result.FiberPer100G);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNotFound_ReturnsNull() {
        string json = """{"status": 0, "product": null}""";
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNameEmpty_ReturnsNull() {
        string json = """
            {
              "status": 1,
              "product": {
                "product_name": "",
                "brands": "Test",
                "nutriments": {}
              }
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("1234567890123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenTransportFails_ReturnsNull() {
        OpenFoodFactsService service = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_EncodesBarcodePathSegment() {
        var handler = new RecordingHttpMessageHandler("""{"status": 0, "product": null}""");
        OpenFoodFactsService service = CreateService(handler);

        await service.GetByBarcodeAsync(" 123/456 ");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api/v2/product/123%2F456", handler.LastRequestUri!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenJsonInvalid_ReturnsNull() {
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler("not json"));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenServerReturns500_ReturnsNull() {
        OpenFoodFactsService service = CreateService(new ErrorHttpMessageHandler(HttpStatusCode.InternalServerError));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WithNullNutriments_ReturnsProductWithNullNutrition() {
        string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Mystery Product",
                "nutriments": null
              }
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("1111111111111");

        Assert.NotNull(result);
        Assert.Equal("Mystery Product", result.Name);
        Assert.Null(result.CaloriesPer100G);
        Assert.Null(result.ProteinsPer100G);
    }

    [Fact]
    public async Task GetByBarcodeAsync_TrimsWhitespaceFromBrandAndCategory() {
        string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Test",
                "brands": "  Brand  ",
                "categories": "  Cat  ",
                "nutriments": {}
              }
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("1234567890123");

        Assert.NotNull(result);
        Assert.Equal("Brand", result.Brand);
        Assert.Equal("Cat", result.Category);
    }

    [Fact]
    public async Task SearchAsync_WhenProductsFound_ReturnsMappedList() {
        string json = """
            {
              "products": [
                {
                  "code": "111",
                  "product_name": "Milk",
                  "brands": "Brand A",
                  "nutriments": { "energy-kcal_100g": 60, "proteins_100g": 3.2 }
                },
                {
                  "code": "222",
                  "product_name": "Yogurt",
                  "nutriments": {}
                }
              ]
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("dairy");

        Assert.Equal(2, result.Count);
        Assert.Equal("111", result[0].Barcode);
        Assert.Equal("Milk", result[0].Name);
        Assert.Equal("Brand A", result[0].Brand);
        Assert.Equal(60, result[0].CaloriesPer100G);
        Assert.Equal("222", result[1].Barcode);
        Assert.Equal("Yogurt", result[1].Name);
    }

    [Fact]
    public async Task SearchAsync_FiltersOutProductsWithNoName() {
        string json = """
            {
              "products": [
                { "code": "111", "product_name": "Valid", "nutriments": {} },
                { "code": "222", "product_name": "", "nutriments": {} },
                { "code": "333", "product_name": null, "nutriments": {} }
              ]
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("test");

        Assert.Single(result);
        Assert.Equal("Valid", result[0].Name);
    }

    [Fact]
    public async Task SearchAsync_WhenTransportFails_ReturnsEmptyList() {
        OpenFoodFactsService service = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("milk");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WhenNullProducts_ReturnsEmptyList() {
        string json = """{"products": null}""";
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("milk");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WhenRepeatedWithinCacheWindow_ReturnsCachedResultWithoutSecondRequest() {
        string json = """
            {
              "products": [
                {
                  "code": "5449000054227",
                  "product_name": "Fanta",
                  "brands": "Coca-Cola",
                  "nutriments": {}
                }
              ]
            }
            """;
        var handler = new CountingHttpMessageHandler(json);
        OpenFoodFactsService service = CreateService(handler);

        IReadOnlyList<OpenFoodFactsProductModel> firstResult = await service.SearchAsync("cached-fanta");
        IReadOnlyList<OpenFoodFactsProductModel> secondResult = await service.SearchAsync("cached-fanta");

        Assert.Single(firstResult);
        Assert.Single(secondResult);
        Assert.Equal("Fanta", secondResult[0].Name);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SearchAsync_ClampsLimitBeforeCallingApi() {
        var handler = new RecordingHttpMessageHandler("""{"products": []}""");
        OpenFoodFactsService service = CreateService(handler);

        await service.SearchAsync("milk", limit: 500);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("page_size=50", handler.LastRequestUri!.Query, StringComparison.Ordinal);
    }

    private static OpenFoodFactsService CreateService(HttpMessageHandler handler) {
        var httpClient = new HttpClient(handler);
        return new OpenFoodFactsService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenFoodFactsApiOptions()),
            NullLogger<OpenFoodFactsService>.Instance);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SuccessHttpMessageHandler(string json) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ErrorHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingHttpMessageHandler(string json) : HttpMessageHandler {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler(string json) : HttpMessageHandler {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
