using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
[Collection("OpenFoodFacts shared state")]
public sealed class OpenFoodFactsExternalFoodServiceTests {

    [Fact]
    public async Task OpenFoodFactsGetByBarcodeAsync_WithValidResponse_MapsProduct() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("/api/v2/product/12345", request.RequestUri!.ToString(), StringComparison.Ordinal);
            return JsonResponse("""
                {
                  "status": 1,
                  "product": {
                    "product_name": " Milk ",
                    "brands": " Farm ",
                    "categories": " Dairy ",
                    "image_url": " https://example.com/milk.jpg ",
                    "nutriments": {
                      "energy-kcal_100g": 60,
                      "proteins_100g": 3.2,
                      "fat_100g": 1.5,
                      "carbohydrates_100g": 4.8,
                      "fiber_100g": 0
                    }
                  }
                }
                """);
        });
        OpenFoodFactsService service = CreateOpenFoodFactsService(handler);

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync(" 12345 ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(" 12345 ", result.Barcode);
        Assert.Equal(" Milk ", result.Name);
        Assert.Equal("Farm", result.Brand);
        Assert.Equal(60, result.CaloriesPer100G);
    }

    [Fact]
    public async Task OpenFoodFactsGetByBarcodeAsync_WhenProductMissingOrUnnamed_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""
            { "status": 1, "product": { "product_name": "   " } }
            """));
        OpenFoodFactsService service = CreateOpenFoodFactsService(handler);

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("12345", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenFoodFactsSearchAsync_WithValidResponse_FiltersAndMapsProducts() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Contains("page_size=2", request.RequestUri!.ToString(), StringComparison.Ordinal);
            return JsonResponse("""
                {
                  "products": [
                    { "code": "1", "product_name": "Milk", "brands": "Farm" },
                    { "code": "", "product_name": "No code" },
                    { "code": "3", "product_name": "   " }
                  ]
                }
                """);
        });
        OpenFoodFactsService service = CreateOpenFoodFactsService(handler);

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("unique-query-for-test", limit: 2, cancellationToken: CancellationToken.None);

        OpenFoodFactsProductModel product = Assert.Single(result);
        Assert.Equal("1", product.Barcode);
        Assert.Equal("Milk", product.Name);
        Assert.Equal("Farm", product.Brand);
    }

    [Fact]
    public async Task OpenFoodFactsSearchAsync_WhenRequestFails_ReturnsEmpty() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        OpenFoodFactsService service = CreateOpenFoodFactsService(handler);

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync(Guid.NewGuid().ToString("N"), cancellationToken: CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task OpenFoodFactsSearchAsync_WhenRequestFailsWithStaleCache_ReturnsCachedProducts() {
        string query = $"stale-{Guid.NewGuid():N}";
        const int limit = 3;
        var warmupHandler = new RecordingHttpMessageHandler(_ => JsonResponse("""
            {
              "products": [
                { "code": "cached-1", "product_name": "Cached milk", "brands": "Farm" }
              ]
            }
            """));
        OpenFoodFactsService warmupService = CreateOpenFoodFactsService(warmupHandler);
        IReadOnlyList<OpenFoodFactsProductModel> warmup = await warmupService.SearchAsync(query, limit, CancellationToken.None);
        MakeOpenFoodFactsSearchCacheStale(query, limit, TimeSpan.FromMinutes(30));
        var failingHandler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        OpenFoodFactsService failingService = CreateOpenFoodFactsService(failingHandler);

        IReadOnlyList<OpenFoodFactsProductModel> result = await failingService.SearchAsync(query, limit, CancellationToken.None);

        Assert.Single(warmup);
        OpenFoodFactsProductModel product = Assert.Single(result);
        Assert.Equal("cached-1", product.Barcode);
        Assert.Equal("Cached milk", product.Name);
        Assert.Single(failingHandler.Requests);
    }

    private static OpenFoodFactsService CreateOpenFoodFactsService(RecordingHttpMessageHandler handler) {
        return new OpenFoodFactsService(
            new HttpClient(handler),
            MsOptions.Create(new OpenFoodFactsApiOptions {
                BaseUrl = "https://off.test",
            }),
            FixedTime,
            NullLogger<OpenFoodFactsService>.Instance);
    }

    private static void MakeOpenFoodFactsSearchCacheStale(string query, int limit, TimeSpan age) {
        System.Reflection.FieldInfo field = typeof(OpenFoodFactsService).GetField(
            "SearchCache",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        object cache = field.GetValue(null)!;
        string cacheKey = OpenFoodFactsService.GetSearchCacheKey(query, Math.Clamp(limit, 1, 50));
        object cached = cache.GetType().GetMethod("get_Item")!.Invoke(cache, [cacheKey])!;
        Type cachedType = cached.GetType();
        object products = cachedType.GetProperty("Products")!.GetValue(cached)!;
        object sizeBytes = cachedType.GetProperty("SizeBytes")!.GetValue(cached)!;
        object stale = Activator.CreateInstance(cachedType, FixedNow - age, products, sizeBytes)!;
        cache.GetType().GetMethod("set_Item")!.Invoke(cache, [cacheKey, stale]);
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
