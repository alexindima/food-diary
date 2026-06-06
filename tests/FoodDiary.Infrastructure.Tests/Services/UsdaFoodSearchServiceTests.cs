using System.Net;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UsdaFoodSearchServiceTests {
    [Fact]
    public async Task GetFoodDetailAsync_WhenBrandedFoodFound_ReturnsMappedNutrients() {
        string json = """
            {
              "fdcId": 539789,
              "description": "FANTA, SODA, RASPBERRY & PASSIONFRUIT",
              "brandName": "FANTA",
              "foodCategory": {
                "description": "Soda"
              },
              "foodNutrients": [
                {
                  "amount": 48,
                  "nutrient": {
                    "id": 1008,
                    "name": "Energy",
                    "unitName": "KCAL"
                  }
                },
                {
                  "amount": 12.7,
                  "nutrient": {
                    "id": 1005,
                    "name": "Carbohydrate, by difference",
                    "unitName": "G"
                  }
                }
              ],
              "foodPortions": [
                {
                  "id": 1,
                  "amount": 1,
                  "gramWeight": 355,
                  "portionDescription": "can",
                  "measureUnit": {
                    "name": "serving"
                  }
                }
              ]
            }
            """;
        UsdaFoodSearchService service = CreateService(new SuccessHttpMessageHandler(json));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.NotNull(result);
        Assert.Equal(539789, result.FdcId);
        Assert.Equal("FANTA, SODA, RASPBERRY & PASSIONFRUIT", result.Description);
        Assert.Equal("Soda", result.FoodCategory);
        Assert.Equal(2, result.Nutrients.Count);
        Assert.Equal(1008, result.Nutrients[0].NutrientId);
        Assert.Equal(48, result.Nutrients[0].AmountPer100g);
        Assert.Single(result.Portions);
        Assert.Equal(355, result.Portions[0].GramWeight);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenNotFound_ReturnsNull() {
        UsdaFoodSearchService service = CreateService(new ErrorHttpMessageHandler(HttpStatusCode.NotFound));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.Null(result);
    }

    private static UsdaFoodSearchService CreateService(HttpMessageHandler handler) {
        var httpClient = new HttpClient(handler);
        return new UsdaFoodSearchService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new UsdaApiOptions {
                ApiKey = "test-key",
            }),
            NullLogger<UsdaFoodSearchService>.Instance);
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
    private sealed class ErrorHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
