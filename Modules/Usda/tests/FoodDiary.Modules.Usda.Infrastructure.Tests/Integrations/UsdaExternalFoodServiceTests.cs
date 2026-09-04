using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class UsdaExternalFoodServiceTests {
    [Fact]
    public async Task UsdaSearchBrandedAsync_WhenApiKeyMissing_ReturnsEmptyWithoutSendingRequest() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        UsdaFoodSearchService service = CreateUsdaService(handler, apiKey: "");

        IReadOnlyList<UsdaFoodModel> result = await service.SearchBrandedAsync("milk", cancellationToken: CancellationToken.None);

        Assert.Empty(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UsdaSearchBrandedAsync_WithValidResponse_MapsFoods() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://usda.test/foods/search?api_key=key", request.RequestUri!.ToString());
            return JsonResponse("""
                {
                  "foods": [
                    { "fdcId": 1, "description": "Milk", "brandName": "Farm" },
                    { "fdcId": 2, "description": "Apple", "foodCategory": "Fruit" }
                  ]
                }
                """);
        });
        UsdaFoodSearchService service = CreateUsdaService(handler);

        IReadOnlyList<UsdaFoodModel> result = await service.SearchBrandedAsync("milk", limit: 2, cancellationToken: CancellationToken.None);

        Assert.Collection(
            result,
            food => {
                Assert.Equal(1, food.FdcId);
                Assert.Equal("Milk", food.Description);
                Assert.Equal("Farm", food.FoodCategory);
            },
            food => {
                Assert.Equal(2, food.FdcId);
                Assert.Equal("Apple", food.Description);
                Assert.Equal("Fruit", food.FoodCategory);
            });
    }

    [Fact]
    public async Task UsdaSearchBrandedAsync_WhenRequestFails_ReturnsEmpty() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        UsdaFoodSearchService service = CreateUsdaService(handler);

        IReadOnlyList<UsdaFoodModel> result = await service.SearchBrandedAsync("milk", cancellationToken: CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UsdaGetFoodDetailAsync_WhenNotFound_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        UsdaFoodSearchService service = CreateUsdaService(handler);

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(123, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UsdaGetFoodDetailAsync_WithValidResponse_MapsDetail() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://usda.test/food/123?api_key=key", request.RequestUri!.ToString());
            return JsonResponse("""
                {
                  "fdcId": 123,
                  "description": "Milk",
                  "foodCategory": { "description": "Dairy" },
                  "foodNutrients": [
                    { "amount": 3.2, "nutrient": { "id": 1003, "name": "Protein", "unitName": "g" } },
                    { "amount": null, "nutrient": { "id": 1004, "name": "Fat", "unitName": "g" } }
                  ],
                  "foodPortions": [
                    { "id": null, "amount": null, "gramWeight": null, "portionDescription": "Cup", "modifier": "cup", "measureUnit": null }
                  ]
                }
                """);
        });
        UsdaFoodSearchService service = CreateUsdaService(handler);

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(123, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(123, result.FdcId);
        Assert.Equal("Dairy", result.FoodCategory);
        MicronutrientModel nutrient = Assert.Single(result.Nutrients);
        Assert.Equal(1003, nutrient.NutrientId);
        UsdaFoodPortionModel portion = Assert.Single(result.Portions);
        Assert.Equal(1, portion.Id);
        Assert.Equal(1, portion.Amount);
        Assert.Equal("cup", portion.MeasureUnitName);
        Assert.Equal(0, portion.GramWeight);
    }

    private static UsdaFoodSearchService CreateUsdaService(RecordingHttpMessageHandler handler, string apiKey = "key") {
        return new UsdaFoodSearchService(
            new HttpClient(handler),
            MsOptions.Create(new UsdaApiOptions {
                ApiKey = apiKey,
                BaseUrl = "https://usda.test",
            }),
            new UsdaFoodDetailCache(TimeProvider.System),
            NullLogger<UsdaFoodSearchService>.Instance);
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
