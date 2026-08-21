using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsServiceTests {
    private const string IntegrationsMeterName = "FoodDiary.Integrations";

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductFound_ReturnsMappedProduct() {
        const string json = """
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
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("4600000000001");

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
    public async Task GetByBarcodeAsync_WhenProductFound_RecordsExternalProviderTelemetry() {
        string? provider = null;
        string? operation = null;
        string? outcome = null;
        double? durationMs = null;
        using MeterListener listener = CreateExternalProviderListener(
            onRequest: (_, tags) => {
                provider = GetTagValue(tags, "fooddiary.external_provider");
                operation = GetTagValue(tags, "fooddiary.external_provider.operation");
                outcome = GetTagValue(tags, "fooddiary.external_provider.outcome");
            },
            onDuration: (value, tags) => {
                durationMs = value;
                provider ??= GetTagValue(tags, "fooddiary.external_provider");
            });
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Milk",
                "nutriments": {}
              }
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        await service.GetByBarcodeAsync("4600000000001");

        Assert.Equal("open_food_facts", provider);
        Assert.Equal("barcode_lookup", operation);
        Assert.Equal("success", outcome);
        Assert.True(durationMs >= 0);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNotFound_ReturnsNull() {
        const string json = """{"status": 0, "product": null}""";
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        OpenFoodFactsProductModel? result = await service.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenProductNameEmpty_ReturnsNull() {
        const string json = """
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
    public async Task GetByBarcodeAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        OpenFoodFactsService service = CreateService(new CanceledHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetByBarcodeAsync("4600000000001", cancellationTokenSource.Token));
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
        const string json = """
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
        const string json = """
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
        const string json = """
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
    public async Task SearchAsync_WhenProviderReturnsMoreThanLimit_TruncatesResult() {
        const string json = """
            {
              "products": [
                { "code": "111", "product_name": "Milk", "nutriments": {} },
                { "code": "222", "product_name": "Yogurt", "nutriments": {} },
                { "code": "333", "product_name": "Cheese", "nutriments": {} }
              ]
            }
            """;
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync(
            $"limited-{Guid.NewGuid():N}",
            limit: 2);

        Assert.Equal(
            ["111", "222"],
            result.Select(static product => product.Barcode),
            StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_ReturnsReadOnlyProductSnapshot() {
        const string json = """{"products":[{"code":"111","product_name":"Milk","nutriments":{}}]}""";
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync($"immutable-{Guid.NewGuid():N}");

        IList<OpenFoodFactsProductModel> list = Assert.IsAssignableFrom<IList<OpenFoodFactsProductModel>>(result);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = result[0]);
    }

    [Fact]
    public void GetSearchCacheKey_DoesNotRetainNormalizedQueryText() {
        string query = $"Private Search {Guid.NewGuid():N}";

        string cacheKey = OpenFoodFactsService.GetSearchCacheKey(query, 10);
        string equivalentCacheKey = OpenFoodFactsService.GetSearchCacheKey($"  {query.ToUpperInvariant()}  ", 10);

        Assert.Equal(cacheKey, equivalentCacheKey);
        Assert.DoesNotContain(query.Trim().ToLowerInvariant(), cacheKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_FiltersOutProductsWithNoName() {
        const string json = """
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
    public async Task SearchAsync_WhenTransportFails_DoesNotLogRawQuery() {
        string query = $"private-food-query-{Guid.NewGuid():N}";
        var logger = new RecordingLogger<OpenFoodFactsService>();
        OpenFoodFactsService service = CreateService(
            new ThrowingHttpMessageHandler(new HttpRequestException("network error")),
            logger);

        await service.SearchAsync(query);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(query, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains($"QueryLength={query.Length}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenTransportFails_DoesNotLogRawBarcode() {
        string barcode = $"private-barcode-{Guid.NewGuid():N}";
        var logger = new RecordingLogger<OpenFoodFactsService>();
        OpenFoodFactsService service = CreateService(
            new ThrowingHttpMessageHandler(new HttpRequestException("network error")),
            logger);

        await service.GetByBarcodeAsync(barcode);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(barcode, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains($"BarcodeLength={barcode.Length}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_WhenTransportFailsWithStaleCache_ReturnsCachedResult() {
        string query = $"stale-{Guid.NewGuid():N}";
        const string json = """
            {
              "products": [
                { "code": "111", "product_name": "Cached milk", "nutriments": {} }
              ]
            }
            """;
        OpenFoodFactsService warmup = CreateService(new SuccessHttpMessageHandler(json));
        OpenFoodFactsService failing = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        IReadOnlyList<OpenFoodFactsProductModel> cached = await warmup.SearchAsync(query);
        MakeOpenFoodFactsSearchCacheStale(query, limit: 10, TimeSpan.FromMinutes(30));
        IReadOnlyList<OpenFoodFactsProductModel> result = await failing.SearchAsync(query);

        Assert.Single(cached);
        OpenFoodFactsProductModel product = Assert.Single(result);
        Assert.Equal("Cached milk", product.Name);
    }

    [Fact]
    public async Task SearchAsync_WhenTransportFailsWithStaleCache_RecordsStaleCacheTelemetry() {
        string? outcome = null;
        string? errorType = null;
        using MeterListener listener = CreateExternalProviderListener(
            onRequest: (_, tags) => {
                if (string.Equals(GetTagValue(tags, "fooddiary.external_provider.operation"), "search", StringComparison.Ordinal)) {
                    outcome = GetTagValue(tags, "fooddiary.external_provider.outcome");
                    errorType = GetTagValue(tags, "error.type");
                }
            },
            onDuration: null);
        string query = $"stale-telemetry-{Guid.NewGuid():N}";
        const string json = """
            {
              "products": [
                { "code": "111", "product_name": "Cached milk", "nutriments": {} }
              ]
            }
            """;
        OpenFoodFactsService warmup = CreateService(new SuccessHttpMessageHandler(json));
        OpenFoodFactsService failing = CreateService(new ThrowingHttpMessageHandler(new HttpRequestException("network error")));

        await warmup.SearchAsync(query);
        MakeOpenFoodFactsSearchCacheStale(query, limit: 10, TimeSpan.FromMinutes(30));
        await failing.SearchAsync(query);

        Assert.Equal("stale_cache", outcome);
        Assert.Equal(nameof(HttpRequestException), errorType);
    }

    [Fact]
    public async Task SearchAsync_WhenNullProducts_ReturnsEmptyList() {
        const string json = """{"products": null}""";
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler(json));

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync("milk");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WhenRepeatedWithinCacheWindow_ReturnsCachedResultWithoutSecondRequest() {
        const string json = """
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
    public async Task SearchAsync_WhenConcurrentCacheMissesAreIdentical_SendsOneProviderRequest() {
        const string json = """{"products":[{"code":"111","product_name":"Milk","nutriments":{}}]}""";
        var handler = new BlockingCountingHttpMessageHandler(json);
        OpenFoodFactsService service = CreateService(handler);
        string query = $"concurrent-{Guid.NewGuid():N}";

        Task<IReadOnlyList<OpenFoodFactsProductModel>> first = service.SearchAsync(query);
        await handler.RequestStarted.Task;
        Task<IReadOnlyList<OpenFoodFactsProductModel>> second = service.SearchAsync(query);
        handler.ReleaseResponse();
        await Task.WhenAll(first, second);

        Assert.Multiple(
            () => Assert.Equal(1, handler.RequestCount),
            () => Assert.Single(first.Result),
            () => Assert.Single(second.Result));
    }

    [Fact]
    public async Task SearchAsync_WhenManyUniqueRequestsAreInFlight_BoundsSharedStateAndProviderConcurrency() {
        long rejectionCount = 0;
        string? rejectionProvider = null;
        string? rejectionOperation = null;
        string? rejectionReason = null;
        string? rejectionFallback = null;
        using MeterListener listener = CreateExternalProviderListener(
            onRejection: (value, tags) => {
                Interlocked.Add(ref rejectionCount, value);
                rejectionProvider = GetTagValue(tags, "fooddiary.external_provider");
                rejectionOperation = GetTagValue(tags, "fooddiary.external_provider.operation");
                rejectionReason = GetTagValue(tags, "fooddiary.external_provider.rejection_reason");
                rejectionFallback = GetTagValue(tags, "fooddiary.external_provider.fallback");
            });
        var handler = new ConcurrencyTrackingHttpMessageHandler(OpenFoodFactsService.MaxConcurrentSearches);
        OpenFoodFactsService service = CreateService(handler);
        string queryPrefix = Guid.NewGuid().ToString("N");
        const int requestCount = OpenFoodFactsService.MaxInFlightSearches + 8;

        Task<IReadOnlyList<OpenFoodFactsProductModel>>[] requests = [
            .. Enumerable.Range(0, requestCount)
                .Select(index => service.SearchAsync(
                    string.Create(CultureInfo.InvariantCulture, $"{queryPrefix}-{index}"))),
        ];
        try {
            await handler.ConcurrencyLimitReached.Task.WaitAsync(TimeSpan.FromSeconds(2), TimeProvider.System);
            Assert.Multiple(
                () => Assert.InRange(handler.MaxObservedConcurrency, 1, OpenFoodFactsService.MaxConcurrentSearches),
                () => Assert.InRange(
                    OpenFoodFactsService.InFlightSearchCount,
                    1,
                    OpenFoodFactsService.MaxInFlightSearches));
        } finally {
            handler.ReleaseResponses();
        }

        await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        Assert.Multiple(
            () => Assert.Equal(OpenFoodFactsService.MaxInFlightSearches, handler.RequestCount),
            () => Assert.Equal(0, OpenFoodFactsService.InFlightSearchCount),
            () => Assert.Equal(requestCount - OpenFoodFactsService.MaxInFlightSearches, rejectionCount),
            () => Assert.Equal("open_food_facts", rejectionProvider),
            () => Assert.Equal("search", rejectionOperation),
            () => Assert.Equal("in_flight_limit", rejectionReason),
            () => Assert.Equal("empty", rejectionFallback),
            () => Assert.All(
                requests.Skip(OpenFoodFactsService.MaxInFlightSearches),
                static request => Assert.Empty(request.Result)));
    }

    [Fact]
    public async Task SearchAsync_ClampsLimitBeforeCallingApi() {
        var handler = new RecordingHttpMessageHandler("""{"products": []}""");
        OpenFoodFactsService service = CreateService(handler);

        await service.SearchAsync("milk", limit: 500);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("page_size=50", handler.LastRequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryExceedsLimit_ReturnsEmptyWithoutProviderRequest() {
        var handler = new CountingHttpMessageHandler("""{"products": []}""");
        OpenFoodFactsService service = CreateService(handler);

        IReadOnlyList<OpenFoodFactsProductModel> result = await service.SearchAsync(
            new string('x', OpenFoodFactsService.MaxSearchQueryLength + 1));

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SearchAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        OpenFoodFactsService service = CreateService(new CanceledHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchAsync($"cancel-{Guid.NewGuid():N}", cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SearchAsync_WhenCacheCapacityIsExceeded_EvictsOldEntries() {
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler("""{"products": []}"""));
        string queryPrefix = Guid.NewGuid().ToString("N");

        for (int index = 0; index <= OpenFoodFactsService.MaxSearchCacheEntries; index++) {
            await service.SearchAsync(string.Create(CultureInfo.InvariantCulture, $"{queryPrefix}-{index}"));
        }

        Assert.InRange(OpenFoodFactsService.SearchCacheEntryCount, 1, OpenFoodFactsService.MaxSearchCacheEntries);
        Assert.InRange(OpenFoodFactsService.SearchCacheSizeBytes, 0, OpenFoodFactsService.MaxSearchCacheSizeBytes);
    }

    [Fact]
    public async Task SearchAsync_WhenCacheByteBudgetIsExceeded_EvictsOldEntries() {
        string largeProductName = new('x', 700_000);
        string json = System.Text.Json.JsonSerializer.Serialize(new {
            products = new[] {
                new {
                    code = "111",
                    product_name = largeProductName,
                    nutriments = new { },
                },
            },
        });
        OpenFoodFactsService service = CreateServiceWithTimeProvider(
            new SuccessHttpMessageHandler(json),
            new IncrementingTimeProvider());
        string queryPrefix = Guid.NewGuid().ToString("N");
        const int requestedEntryCount = 25;

        for (int index = 0; index < requestedEntryCount; index++) {
            await service.SearchAsync(string.Create(CultureInfo.InvariantCulture, $"{queryPrefix}-{index}"));
        }

        Assert.Multiple(
            () => Assert.InRange(OpenFoodFactsService.SearchCacheSizeBytes, 1, OpenFoodFactsService.MaxSearchCacheSizeBytes),
            () => Assert.True(OpenFoodFactsService.SearchCacheEntryCount < requestedEntryCount));
    }

    [Theory]
    [InlineData("timeout", false, typeof(TaskCanceledException))]
    [InlineData("json_error", false, typeof(System.Text.Json.JsonException))]
    [InlineData("oversized_response", false, typeof(InvalidDataException))]
    [InlineData("response_body_timeout", false, typeof(TimeoutException))]
    [InlineData("transport_error", false, typeof(HttpRequestException))]
    [InlineData("failure", false, typeof(InvalidOperationException))]
    public void ResolveFailureOutcome_MapsExceptionTypeToOutcome(
        string expectedOutcome,
        bool cancelToken,
        Type exceptionType) {
        using var cancellationTokenSource = new CancellationTokenSource();
        if (cancelToken) {
            cancellationTokenSource.Cancel();
        }

        Exception exception = exceptionType == typeof(HttpRequestException)
            ? new HttpRequestException("network error")
            : (Exception)Activator.CreateInstance(exceptionType, "failed")!;

        string outcome = InvokeResolveFailureOutcome(exception, cancellationTokenSource.Token);

        Assert.Equal(expectedOutcome, outcome);
    }

    [Fact]
    public void ResolveFailureOutcome_WithCanceledToken_ReturnsCanceled() {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        string outcome = InvokeResolveFailureOutcome(new TaskCanceledException("canceled"), cancellationTokenSource.Token);

        Assert.Equal("canceled", outcome);
    }

    [Fact]
    public void ResolveFailureOutcome_WithHttpStatusCode_ReturnsHttpError() {
        var exception = new HttpRequestException("server error", inner: null, HttpStatusCode.InternalServerError);

        string outcome = InvokeResolveFailureOutcome(exception, CancellationToken.None);

        Assert.Equal("http_error", outcome);
    }

    [Fact]
    public void RemoveCachedSearch_WhenEntryWasReplaced_PreservesReplacement() {
        System.Reflection.FieldInfo cacheField = typeof(OpenFoodFactsService).GetField(
            "SearchCache",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var cache = (System.Collections.IDictionary)cacheField.GetValue(null)!;
        Type cachedType = typeof(OpenFoodFactsService).GetNestedType(
            "CachedSearchResult",
            System.Reflection.BindingFlags.NonPublic)!;
        IReadOnlyList<OpenFoodFactsProductModel> products = Array.Empty<OpenFoodFactsProductModel>();
        object staleEntry = Activator.CreateInstance(cachedType, FixedNow - TimeSpan.FromHours(7), products, 0L)!;
        object replacement = Activator.CreateInstance(cachedType, FixedNow, products, 0L)!;
        string cacheKey = OpenFoodFactsService.GetSearchCacheKey($"replacement-{Guid.NewGuid():N}", 10);
        try {
            cache[cacheKey] = staleEntry;
            cache[cacheKey] = replacement;
            System.Reflection.MethodInfo removeMethod = typeof(OpenFoodFactsService).GetMethod(
                "RemoveCachedSearch",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

            removeMethod.Invoke(null, [cacheKey, staleEntry]);

            Assert.Same(replacement, cache[cacheKey]);
        } finally {
            cache.Remove(cacheKey);
        }
    }

    [Fact]
    public async Task SearchAsync_WhenCacheEntryExpired_RemovesItAndRefreshesFromProvider() {
        string query = $"expired-{Guid.NewGuid():N}";
        OpenFoodFactsService warmup = CreateService(
            new SuccessHttpMessageHandler("""{"products":[{"code":"old","product_name":"Old","nutriments":{}}]}"""));
        await warmup.SearchAsync(query);
        MakeOpenFoodFactsSearchCacheStale(query, limit: 10, TimeSpan.FromHours(7));
        OpenFoodFactsService refreshed = CreateService(
            new SuccessHttpMessageHandler("""{"products":[{"code":"new","product_name":"New","nutriments":{}}]}"""));

        OpenFoodFactsProductModel product = Assert.Single(await refreshed.SearchAsync(query));

        Assert.Equal("new", product.Barcode);
    }

    [Fact]
    public void CacheSearchResult_WhenEntryIsReplaced_UpdatesCacheSize() {
        OpenFoodFactsService service = CreateService(new SuccessHttpMessageHandler("{}"));
        string key = OpenFoodFactsService.GetSearchCacheKey($"replace-{Guid.NewGuid():N}", 10);
        System.Reflection.MethodInfo method = typeof(OpenFoodFactsService).GetMethod(
            "CacheSearchResult",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        IReadOnlyList<OpenFoodFactsProductModel> first = [CreateProductModel("1", "A")];
        IReadOnlyList<OpenFoodFactsProductModel> second = [CreateProductModel("2", new string('x', 100))];

        method.Invoke(service, [key, first]);
        long firstSize = OpenFoodFactsService.SearchCacheSizeBytes;
        method.Invoke(service, [key, second]);

        Assert.True(OpenFoodFactsService.SearchCacheSizeBytes > firstSize);
    }

    private static OpenFoodFactsProductModel CreateProductModel(string barcode, string name) =>
        new(
            Barcode: barcode,
            Name: name,
            Brand: null,
            Category: null,
            ImageUrl: null,
            CaloriesPer100G: null,
            ProteinsPer100G: null,
            FatsPer100G: null,
            CarbsPer100G: null,
            FiberPer100G: null);

    private static OpenFoodFactsService CreateService(
        HttpMessageHandler handler,
        ILogger<OpenFoodFactsService>? logger = null) {
        var httpClient = new HttpClient(handler);
        return new OpenFoodFactsService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenFoodFactsApiOptions()),
            FixedTime,
            logger ?? NullLogger<OpenFoodFactsService>.Instance);
    }

    private static OpenFoodFactsService CreateServiceWithTimeProvider(
        HttpMessageHandler handler,
        TimeProvider timeProvider) {
        var httpClient = new HttpClient(handler);
        return new OpenFoodFactsService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenFoodFactsApiOptions()),
            timeProvider,
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

    private static string InvokeResolveFailureOutcome(Exception exception, CancellationToken cancellationToken) {
        System.Reflection.MethodInfo method = typeof(OpenFoodFactsService).GetMethod(
            "ResolveFailureOutcome",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, [exception, cancellationToken])!;
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class IncrementingTimeProvider : TimeProvider {
        private long _offsetTicks;

        public override DateTimeOffset GetUtcNow() =>
            FixedNow.AddTicks(Interlocked.Increment(ref _offsetTicks));
    }

    private static MeterListener CreateExternalProviderListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onRequest = null,
        Action<double, ReadOnlySpan<KeyValuePair<string, object?>>>? onDuration = null,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onRejection = null) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, IntegrationsMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name is "fooddiary.external_provider.requests" or
                "fooddiary.external_provider.duration" or
                "fooddiary.external_provider.rejections") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.external_provider.requests", StringComparison.Ordinal)) {
                onRequest?.Invoke(value, tags);
            } else if (string.Equals(instrument.Name, "fooddiary.external_provider.rejections", StringComparison.Ordinal)) {
                onRejection?.Invoke(value, tags);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.external_provider.duration", StringComparison.Ordinal)) {
                onDuration?.Invoke(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
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
    private sealed class CanceledHttpMessageHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
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
    private sealed class BlockingCountingHttpMessageHandler(string json) : HttpMessageHandler {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int RequestCount { get; private set; }

        public void ReleaseResponse() => _release.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestCount++;
            RequestStarted.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ConcurrencyTrackingHttpMessageHandler(int expectedConcurrency) : HttpMessageHandler {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeRequests;
        private int _maxObservedConcurrency;
        private int _requestCount;

        public TaskCompletionSource ConcurrencyLimitReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);
        public int RequestCount => Volatile.Read(ref _requestCount);

        public void ReleaseResponses() => _release.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Interlocked.Increment(ref _requestCount);
            int activeRequests = Interlocked.Increment(ref _activeRequests);
            UpdateMaxObservedConcurrency(activeRequests);
            if (activeRequests >= expectedConcurrency) {
                ConcurrencyLimitReached.TrySetResult();
            }

            try {
                await _release.Task.WaitAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("""{"products":[]}""", System.Text.Encoding.UTF8, "application/json"),
                };
            } finally {
                Interlocked.Decrement(ref _activeRequests);
            }
        }

        private void UpdateMaxObservedConcurrency(int activeRequests) {
            int observed = Volatile.Read(ref _maxObservedConcurrency);
            while (activeRequests > observed) {
                int previous = Interlocked.CompareExchange(ref _maxObservedConcurrency, activeRequests, observed);
                if (previous == observed) {
                    return;
                }

                observed = previous;
            }
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
