using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services;

internal sealed class OpenFoodFactsService(
    HttpClient httpClient,
    IOptions<OpenFoodFactsApiOptions> options,
    TimeProvider timeProvider,
    ILogger<OpenFoodFactsService> logger) : IOpenFoodFactsService {
    private const string ProviderName = "open_food_facts";
    private const string BarcodeLookupOperation = "barcode_lookup";
    private const string SearchOperation = "search";
    internal const int MaxSearchCacheEntries = 1024;
    internal const long MaxSearchCacheSizeBytes = 16 * 1024 * 1024;
    internal const int MaxConcurrentSearches = 16;
    internal const int MaxInFlightSearches = 128;
    internal const int MaxSearchQueryLength = 256;
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StaleSearchCacheTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan SearchOperationTimeout = TimeSpan.FromSeconds(15);
    private static readonly ConcurrentDictionary<string, CachedSearchResult> SearchCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>> InFlightSearches = new(StringComparer.Ordinal);
    private static readonly SemaphoreSlim SearchConcurrencyGate = new(MaxConcurrentSearches, MaxConcurrentSearches);
    private static readonly byte[] SearchCacheKeySecret = RandomNumberGenerator.GetBytes(32);
    private static readonly Lock SearchCacheLock = new();
    private static long SearchCacheSizeBytesValue;
    private static int InFlightSearchCountValue;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    public async Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        string outcome = "success";
        string? errorType = null;
        try {
            string baseUrl = options.Value.BaseUrl.TrimEnd('/');
            string encodedBarcode = Uri.EscapeDataString(barcode.Trim());
            string url = $"{baseUrl}/api/v2/product/{encodedBarcode}?fields=code,product_name,brands,categories,image_url,nutriments";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OffApiResponse? result = await BoundedHttpContentReader.ReadFromJsonAsync<OffApiResponse>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
            if (result?.Status != 1 || result.Product is null) {
                outcome = "not_found";
                return null;
            }

            OffProduct p = result.Product;
            string? name = p.ProductName;
            if (!string.IsNullOrWhiteSpace(name)) {
                return new OpenFoodFactsProductModel(
                    barcode,
                    name,
                    NullIfEmpty(p.Brands),
                    NullIfEmpty(p.Categories),
                    NullIfEmpty(p.ImageUrl),
                    p.Nutriments?.EnergyKcal100G,
                    p.Nutriments?.Proteins100G,
                    p.Nutriments?.Fat100G,
                    p.Nutriments?.Carbohydrates100G,
                    p.Nutriments?.Fiber100G);
            }

            outcome = "empty";
            return null;

        } catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested) {
            outcome = "canceled";
            errorType = ex.GetType().Name;
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException or TimeoutException) {
            outcome = ResolveFailureOutcome(ex, cancellationToken);
            errorType = ex.GetType().Name;
            logger.LogWarning(ex, "Open Food Facts lookup failed for barcode '{Barcode}'", barcode);
            return null;
        } finally {
            RecordRequestTelemetry(BarcodeLookupOperation, outcome, stopwatch, errorType);
        }
    }

    public async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        string normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length is 0 or > MaxSearchQueryLength) {
            return [];
        }

        int normalizedLimit = Math.Clamp(limit, 1, 50);
        string cacheKey = GetSearchCacheKey(normalizedQuery, normalizedLimit);
        if (TryGetCachedSearch(cacheKey, SearchCacheTtl, out IReadOnlyList<OpenFoodFactsProductModel> freshProducts)) {
            return freshProducts;
        }

        while (true) {
            if (InFlightSearches.TryGetValue(
                    cacheKey,
                    out Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>? existing)) {
                return await existing.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            int reservedCount = Interlocked.Increment(ref InFlightSearchCountValue);
            if (reservedCount > MaxInFlightSearches) {
                Interlocked.Decrement(ref InFlightSearchCountValue);
                return GetOverloadedSearchFallback(cacheKey);
            }

            var candidate = new Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>(
                () => SearchProviderAsync(normalizedQuery, normalizedLimit, cacheKey, CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication);
            if (!InFlightSearches.TryAdd(cacheKey, candidate)) {
                Interlocked.Decrement(ref InFlightSearchCountValue);
                continue;
            }

            Task<IReadOnlyList<OpenFoodFactsProductModel>> pendingTask = candidate.Value;
            _ = pendingTask.ContinueWith(
                static (_, state) => {
                    (string key, Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>> expected) =
                        ((string, Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>))state!;
                    if (InFlightSearches.TryGetValue(
                            key,
                            out Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>? current) &&
                        ReferenceEquals(current, expected) &&
                        InFlightSearches.TryRemove(
                            key,
                            out Lazy<Task<IReadOnlyList<OpenFoodFactsProductModel>>>? removed) &&
                        ReferenceEquals(removed, expected)) {
                        Interlocked.Decrement(ref InFlightSearchCountValue);
                    }
                },
                (cacheKey, candidate),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return await pendingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private IReadOnlyList<OpenFoodFactsProductModel> GetOverloadedSearchFallback(string cacheKey) {
        bool hasStaleProducts = TryGetCachedSearch(
            cacheKey,
            StaleSearchCacheTtl,
            out IReadOnlyList<OpenFoodFactsProductModel> staleProducts);
        IntegrationsTelemetry.RecordExternalProviderRejection(
            ProviderName,
            SearchOperation,
            "in_flight_limit",
            hasStaleProducts ? "stale_cache" : "empty");
        return hasStaleProducts ? staleProducts : [];
    }

    private async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchProviderAsync(
        string query,
        int normalizedLimit,
        string cacheKey,
        CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        string outcome = "success";
        string? errorType = null;
        bool concurrencyLeaseAcquired = false;
        using var operationTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        operationTimeout.CancelAfter(SearchOperationTimeout);
        try {
            await SearchConcurrencyGate.WaitAsync(operationTimeout.Token).ConfigureAwait(false);
            concurrencyLeaseAcquired = true;
            string baseUrl = options.Value.BaseUrl.TrimEnd('/');
            string encodedQuery = Uri.EscapeDataString(query);
            string url = string.Create(CultureInfo.InvariantCulture, $"{baseUrl}/cgi/search.pl?search_terms={encodedQuery}&page_size={normalizedLimit}&json=1&fields=code,product_name,brands,categories,image_url,nutriments");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                operationTimeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OffSearchResponse? result = await BoundedHttpContentReader.ReadFromJsonAsync<OffSearchResponse>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                operationTimeout.Token).ConfigureAwait(false);
            if (result?.Products is null) {
                outcome = "empty";
                return [];
            }

            IReadOnlyList<OpenFoodFactsProductModel> products = MapSearchProducts(result.Products, normalizedLimit);
            if (products.Count == 0) {
                outcome = "empty";
            }

            CacheSearchResult(cacheKey, products);
            return products;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            outcome = "canceled";
            errorType = nameof(OperationCanceledException);
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException or TimeoutException) {
            outcome = ResolveFailureOutcome(ex, CancellationToken.None);
            errorType = ex.GetType().Name;
            if (TryGetCachedSearch(cacheKey, StaleSearchCacheTtl, out IReadOnlyList<OpenFoodFactsProductModel> staleProducts)) {
                outcome = "stale_cache";
                logger.LogWarning(ex, "Open Food Facts text search failed; returning cached result. QueryLength={QueryLength}", query.Length);
                return staleProducts;
            }

            logger.LogWarning(ex, "Open Food Facts text search failed. QueryLength={QueryLength}", query.Length);
            return [];
        } finally {
            if (concurrencyLeaseAcquired) {
                SearchConcurrencyGate.Release();
            }
            RecordRequestTelemetry(SearchOperation, outcome, stopwatch, errorType);
        }
    }

    private static void RecordRequestTelemetry(
        string operation,
        string outcome,
        Stopwatch stopwatch,
        string? errorType) {
        stopwatch.Stop();
        IntegrationsTelemetry.RecordExternalProviderRequest(
            ProviderName,
            operation,
            outcome,
            stopwatch.Elapsed.TotalMilliseconds,
            errorType);
    }

    private static string ResolveFailureOutcome(Exception exception, CancellationToken cancellationToken) =>
        exception switch {
            OperationCanceledException => cancellationToken.IsCancellationRequested ? "canceled" : "timeout",
            JsonException => "json_error",
            InvalidDataException => "oversized_response",
            TimeoutException => "response_body_timeout",
            HttpRequestException httpException when httpException.StatusCode is not null => "http_error",
            HttpRequestException => "transport_error",
            _ => "failure",
        };

    private static IReadOnlyList<OpenFoodFactsProductModel> MapSearchProducts(
        List<OffSearchProduct> products,
        int limit) {
        OpenFoodFactsProductModel[] mappedProducts = [
            .. products
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) && !string.IsNullOrWhiteSpace(p.Code))
            .Take(limit)
            .Select(p => new OpenFoodFactsProductModel(
                p.Code!,
                p.ProductName!,
                NullIfEmpty(p.Brands),
                NullIfEmpty(p.Categories),
                NullIfEmpty(p.ImageUrl),
                p.Nutriments?.EnergyKcal100G,
                p.Nutriments?.Proteins100G,
                p.Nutriments?.Fat100G,
                p.Nutriments?.Carbohydrates100G,
                p.Nutriments?.Fiber100G)),
        ];
        return Array.AsReadOnly(mappedProducts);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static string GetSearchCacheKey(string query, int limit) {
        string normalizedQuery = query.Trim().ToLowerInvariant();
        byte[] queryBytes = Encoding.UTF8.GetBytes(normalizedQuery);
        byte[] hash = HMACSHA256.HashData(SearchCacheKeySecret, queryBytes);
        return string.Create(CultureInfo.InvariantCulture, $"{Convert.ToHexString(hash)}:{limit}");
    }

    internal static int SearchCacheEntryCount => SearchCache.Count;

    internal static long SearchCacheSizeBytes {
        get {
            lock (SearchCacheLock) {
                return SearchCacheSizeBytesValue;
            }
        }
    }

    internal static int InFlightSearchCount => Volatile.Read(ref InFlightSearchCountValue);

    private void CacheSearchResult(string cacheKey, IReadOnlyList<OpenFoodFactsProductModel> products) {
        IReadOnlyList<OpenFoodFactsProductModel> snapshot = Array.AsReadOnly([.. products]);
        long sizeBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot).LongLength;
        if (sizeBytes > MaxSearchCacheSizeBytes) {
            return;
        }

        var result = new CachedSearchResult(timeProvider.GetUtcNow(), snapshot, sizeBytes);
        lock (SearchCacheLock) {
            if (SearchCache.TryGetValue(cacheKey, out CachedSearchResult? existing)) {
                SearchCacheSizeBytesValue -= existing.SizeBytes;
            }

            SearchCache[cacheKey] = result;
            SearchCacheSizeBytesValue += result.SizeBytes;
            while (SearchCache.Count > MaxSearchCacheEntries || SearchCacheSizeBytesValue > MaxSearchCacheSizeBytes) {
                KeyValuePair<string, CachedSearchResult> oldest = SearchCache.MinBy(
                    static entry => entry.Value.CachedAt);
                if (SearchCache.TryRemove(oldest.Key, out CachedSearchResult? removed)) {
                    SearchCacheSizeBytesValue -= removed.SizeBytes;
                }
            }
        }
    }

    private bool TryGetCachedSearch(
        string cacheKey,
        TimeSpan maxAge,
        out IReadOnlyList<OpenFoodFactsProductModel> products) {
        if (SearchCache.TryGetValue(cacheKey, out CachedSearchResult? cached)) {
            TimeSpan age = timeProvider.GetUtcNow() - cached.CachedAt;
            if (age <= maxAge) {
                products = cached.Products;
                return true;
            }

            if (age > StaleSearchCacheTtl) {
                RemoveCachedSearch(cacheKey, cached);
            }
        }

        products = [];
        return false;
    }

    private static void RemoveCachedSearch(string cacheKey, CachedSearchResult expected) {
        lock (SearchCacheLock) {
            if (SearchCache.TryGetValue(cacheKey, out CachedSearchResult? current) &&
                ReferenceEquals(current, expected) &&
                SearchCache.TryRemove(cacheKey, out CachedSearchResult? removed)) {
                SearchCacheSizeBytesValue -= removed.SizeBytes;
            }
        }
    }

    private sealed record CachedSearchResult(
        DateTimeOffset CachedAt,
        IReadOnlyList<OpenFoodFactsProductModel> Products,
        long SizeBytes);

    private sealed class OffApiResponse {
        [JsonPropertyName("status")]
        public int Status { get; init; }

        [JsonPropertyName("product")]
        public OffProduct? Product { get; init; }
    }

    private sealed class OffSearchResponse {
        [JsonPropertyName("products")]
        public List<OffSearchProduct>? Products { get; init; }
    }

    private sealed class OffSearchProduct {
        [JsonPropertyName("code")]
        public string? Code { get; init; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; init; }

        [JsonPropertyName("brands")]
        public string? Brands { get; init; }

        [JsonPropertyName("categories")]
        public string? Categories { get; init; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; init; }

        [JsonPropertyName("nutriments")]
        public OffNutriments? Nutriments { get; init; }
    }

    private sealed class OffProduct {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; init; }

        [JsonPropertyName("brands")]
        public string? Brands { get; init; }

        [JsonPropertyName("categories")]
        public string? Categories { get; init; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; init; }

        [JsonPropertyName("nutriments")]
        public OffNutriments? Nutriments { get; init; }
    }

    private sealed class OffNutriments {
        [JsonPropertyName("energy-kcal_100g")]
        public double? EnergyKcal100G { get; init; }

        [JsonPropertyName("proteins_100g")]
        public double? Proteins100G { get; init; }

        [JsonPropertyName("fat_100g")]
        public double? Fat100G { get; init; }

        [JsonPropertyName("carbohydrates_100g")]
        public double? Carbohydrates100G { get; init; }

        [JsonPropertyName("fiber_100g")]
        public double? Fiber100G { get; init; }
    }
}
