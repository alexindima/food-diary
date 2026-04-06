using System.Net;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class OpenFoodFactsServiceTests {
    [Fact]
    public async Task GetByBarcodeAsync_WhenProductFound_ReturnsMappedProduct() {
        var json = """
            {
              "status": 1,
              "product": {
                "product_name": "Молоко 3.2%",
                "brands": "Простоквашино",
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
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.GetByBarcodeAsync("4600000000001");

        Assert.NotNull(result);
        Assert.Equal("4600000000001", result.Barcode);
        Assert.Equal("Молоко 3.2%", result.Name);
        Assert.Equal("Простоквашино", result.Brand);
        Assert.Equal("Dairy", result.Category);
        Assert.Equal(60, result.CaloriesPer100G);
        Assert.Equal(3.2, result.ProteinsPer100G);
        Assert.Equal(3.2, result.FatsPer100G);
        Assert.Equal(4.7, result.CarbsPer100G);
        Assert.Equal(0, result.FiberPer100G);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNotFound_ReturnsNull() {
        var json = """{"status": 0, "product": null}""";
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNameEmpty_ReturnsNull() {
        var json = """
            {
              "status": 1,
              "product": {
                "product_name": "",
                "brands": "Test",
                "nutriments": {}
              }
            }
            """;
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.GetByBarcodeAsync("1234567890123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenTransportFails_ReturnsNull() {
        var service = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        var result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenJsonInvalid_ReturnsNull() {
        var service = CreateService(new SuccessHttpMessageHandler("not json"));

        var result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenServerReturns500_ReturnsNull() {
        var service = CreateService(new ErrorHttpMessageHandler(HttpStatusCode.InternalServerError));

        var result = await service.GetByBarcodeAsync("4600000000001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WithNullNutriments_ReturnsProductWithNullNutrition() {
        var json = """
            {
              "status": 1,
              "product": {
                "product_name": "Mystery Product",
                "nutriments": null
              }
            }
            """;
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.GetByBarcodeAsync("1111111111111");

        Assert.NotNull(result);
        Assert.Equal("Mystery Product", result.Name);
        Assert.Null(result.CaloriesPer100G);
        Assert.Null(result.ProteinsPer100G);
    }

    [Fact]
    public async Task GetByBarcodeAsync_TrimsWhitespaceFromBrandAndCategory() {
        var json = """
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
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.GetByBarcodeAsync("1234567890123");

        Assert.NotNull(result);
        Assert.Equal("Brand", result.Brand);
        Assert.Equal("Cat", result.Category);
    }

    [Fact]
    public async Task SearchAsync_WhenProductsFound_ReturnsMappedList() {
        var json = """
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
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.SearchAsync("dairy");

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
        var json = """
            {
              "products": [
                { "code": "111", "product_name": "Valid", "nutriments": {} },
                { "code": "222", "product_name": "", "nutriments": {} },
                { "code": "333", "product_name": null, "nutriments": {} }
              ]
            }
            """;
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.SearchAsync("test");

        Assert.Single(result);
        Assert.Equal("Valid", result[0].Name);
    }

    [Fact]
    public async Task SearchAsync_WhenTransportFails_ReturnsEmptyList() {
        var service = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        var result = await service.SearchAsync("milk");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WhenNullProducts_ReturnsEmptyList() {
        var json = """{"products": null}""";
        var service = CreateService(new SuccessHttpMessageHandler(json));

        var result = await service.SearchAsync("milk");

        Assert.Empty(result);
    }

    private static OpenFoodFactsService CreateService(HttpMessageHandler handler) {
        var httpClient = new HttpClient(handler);
        return new OpenFoodFactsService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenFoodFactsApiOptions()),
            NullLogger<OpenFoodFactsService>.Instance);
    }

    private sealed class SuccessHttpMessageHandler(string json) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class ErrorHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
