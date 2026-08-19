using System.Net;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UsdaFoodSearchServiceTests {
    [Fact]
    public async Task SearchBrandedAsync_WhenFoodsNull_ReturnsEmpty() {
        UsdaFoodSearchService service = CreateService(new SuccessHttpMessageHandler("""{"foods": null}"""));

        IReadOnlyList<UsdaFoodModel> result = await service.SearchBrandedAsync("milk");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchBrandedAsync_WhenProviderReturnsMoreThanLimit_TruncatesResult() {
        const string json = """
            {
              "foods": [
                { "fdcId": 1, "description": "Milk" },
                { "fdcId": 2, "description": "Yogurt" },
                { "fdcId": 3, "description": "Cheese" }
              ]
            }
            """;
        UsdaFoodSearchService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<UsdaFoodModel> result = await service.SearchBrandedAsync("dairy", limit: 2);

        Assert.Equal([1, 2], result.Select(static food => food.FdcId));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 200)]
    public async Task SearchBrandedAsync_WhenLimitIsOutsideProviderRange_ClampsPageSize(
        int limit,
        int expectedPageSize) {
        var handler = new RequestBodyRecordingHttpMessageHandler();
        UsdaFoodSearchService service = CreateService(handler);

        await service.SearchBrandedAsync("milk", limit);

        using var request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(expectedPageSize, request.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task SearchBrandedAsync_WhenRequestFails_DoesNotLogRawQuery() {
        string query = $"private-food-query-{Guid.NewGuid():N}";
        var logger = new RecordingLogger<UsdaFoodSearchService>();
        UsdaFoodSearchService service = CreateService(
            new ErrorHttpMessageHandler(HttpStatusCode.InternalServerError),
            logger: logger);

        await service.SearchBrandedAsync(query);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(query, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains($"QueryLength={query.Length}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchBrandedAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        UsdaFoodSearchService service = CreateService(new CanceledHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchBrandedAsync("milk", cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenBrandedFoodFound_ReturnsMappedNutrients() {
        const string json = """
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
        Assert.Equal(48, result.Nutrients[0].AmountPer100G);
        Assert.Single(result.Portions);
        Assert.Equal(355, result.Portions[0].GramWeight);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenCategoryObjectMissing_UsesCategoryDescription() {
        const string json = """
            {
              "fdcId": 539789,
              "description": "Legacy food",
              "foodCategoryDescription": "Legacy category",
              "foodNutrients": [],
              "foodPortions": []
            }
            """;
        UsdaFoodSearchService service = CreateService(new SuccessHttpMessageHandler(json));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.NotNull(result);
        Assert.Equal("Legacy category", result.FoodCategory);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenNotFound_ReturnsNull() {
        UsdaFoodSearchService service = CreateService(new ErrorHttpMessageHandler(HttpStatusCode.NotFound));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenApiKeyMissing_ReturnsNullWithoutRequest() {
        var handler = new CountingHttpMessageHandler("""{}""");
        UsdaFoodSearchService service = CreateService(handler, apiKey: "");

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.Null(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenResponseBodyIsNull_ReturnsNull() {
        UsdaFoodSearchService service = CreateService(new SuccessHttpMessageHandler("null"));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenRequestFails_ReturnsNull() {
        UsdaFoodSearchService service = CreateService(new ErrorHttpMessageHandler(HttpStatusCode.InternalServerError));

        UsdaFoodDetailModel? result = await service.GetFoodDetailAsync(539789);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFoodDetailAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        UsdaFoodSearchService service = CreateService(new CanceledHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetFoodDetailAsync(539789, cancellationTokenSource.Token));
    }

    private static UsdaFoodSearchService CreateService(
        HttpMessageHandler handler,
        string apiKey = "test-key",
        ILogger<UsdaFoodSearchService>? logger = null) {
        var httpClient = new HttpClient(handler);
        return new UsdaFoodSearchService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new UsdaApiOptions {
                ApiKey = apiKey,
            }),
            logger ?? NullLogger<UsdaFoodSearchService>.Instance);
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

    [ExcludeFromCodeCoverage]
    private sealed class CanceledHttpMessageHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
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
    private sealed class RequestBodyRecordingHttpMessageHandler : HttpMessageHandler {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("""{"foods": []}"""),
            };
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
