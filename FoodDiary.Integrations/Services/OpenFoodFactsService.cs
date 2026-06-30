using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
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
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StaleSearchCacheTtl = TimeSpan.FromHours(6);
    private static readonly ConcurrentDictionary<string, CachedSearchResult> SearchCache = new(StringComparer.Ordinal);

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

            HttpResponseMessage response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OffApiResponse? result = await response.Content.ReadFromJsonAsync<OffApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result?.Status != 1 || result.Product is null) {
                outcome = "not_found";
                return null;
            }

            OffProduct p = result.Product;
            string? name = p.ProductName;
            if (string.IsNullOrWhiteSpace(name)) {
                outcome = "empty";
                return null;
            }

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
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) {
            outcome = ResolveFailureOutcome(ex, cancellationToken);
            errorType = ex.GetType().Name;
            logger.LogWarning(ex, "Open Food Facts lookup failed for barcode '{Barcode}'", barcode);
            return null;
        } finally {
            stopwatch.Stop();
            IntegrationsTelemetry.RecordExternalProviderRequest(
                ProviderName,
                BarcodeLookupOperation,
                outcome,
                stopwatch.Elapsed.TotalMilliseconds,
                errorType);
        }
    }

    public async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default) {
        int normalizedLimit = Math.Clamp(limit, 1, 50);
        string cacheKey = GetSearchCacheKey(query, normalizedLimit);
        if (TryGetCachedSearch(cacheKey, SearchCacheTtl, out IReadOnlyList<OpenFoodFactsProductModel>? freshProducts)) {
            return freshProducts;
        }

        var stopwatch = Stopwatch.StartNew();
        string outcome = "success";
        string? errorType = null;
        try {
            string baseUrl = options.Value.BaseUrl.TrimEnd('/');
            string encodedQuery = Uri.EscapeDataString(query);
            string url = string.Create(CultureInfo.InvariantCulture, $"{baseUrl}/cgi/search.pl?search_terms={encodedQuery}&page_size={normalizedLimit}&json=1&fields=code,product_name,brands,categories,image_url,nutriments");

            HttpResponseMessage response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OffSearchResponse? result = await response.Content.ReadFromJsonAsync<OffSearchResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result?.Products is null) {
                outcome = "empty";
                return [];
            }

            List<OpenFoodFactsProductModel> products = MapSearchProducts(result.Products);
            if (products.Count == 0) {
                outcome = "empty";
            }

            SearchCache[cacheKey] = new CachedSearchResult(timeProvider.GetUtcNow(), products);
            return products;
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) {
            outcome = ResolveFailureOutcome(ex, cancellationToken);
            errorType = ex.GetType().Name;
            if (TryGetCachedSearch(cacheKey, StaleSearchCacheTtl, out IReadOnlyList<OpenFoodFactsProductModel>? staleProducts)) {
                outcome = "stale_cache";
                logger.LogWarning(ex, "Open Food Facts text search failed for query '{Query}', returning cached result", query);
                return staleProducts;
            }

            logger.LogWarning(ex, "Open Food Facts text search failed for query '{Query}'", query);
            return [];
        } finally {
            stopwatch.Stop();
            IntegrationsTelemetry.RecordExternalProviderRequest(
                ProviderName,
                SearchOperation,
                outcome,
                stopwatch.Elapsed.TotalMilliseconds,
                errorType);
        }
    }

    private static string ResolveFailureOutcome(Exception exception, CancellationToken cancellationToken) =>
        exception switch {
            TaskCanceledException => cancellationToken.IsCancellationRequested ? "canceled" : "timeout",
            System.Text.Json.JsonException => "json_error",
            HttpRequestException httpException when httpException.StatusCode is not null => "http_error",
            HttpRequestException => "transport_error",
            _ => "failure",
        };

    private static List<OpenFoodFactsProductModel> MapSearchProducts(List<OffSearchProduct> products) => [
        .. products
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) && !string.IsNullOrWhiteSpace(p.Code))
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

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetSearchCacheKey(string query, int limit) =>
        string.Create(CultureInfo.InvariantCulture, $"{query.Trim().ToLowerInvariant()}:{limit}");

    private bool TryGetCachedSearch(
        string cacheKey,
        TimeSpan maxAge,
        out IReadOnlyList<OpenFoodFactsProductModel> products) {
        if (SearchCache.TryGetValue(cacheKey, out CachedSearchResult? cached) &&
            timeProvider.GetUtcNow() - cached.CachedAt <= maxAge) {
            products = cached.Products;
            return true;
        }

        products = [];
        return false;
    }

    private sealed record CachedSearchResult(
        DateTimeOffset CachedAt,
        IReadOnlyList<OpenFoodFactsProductModel> Products);

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
