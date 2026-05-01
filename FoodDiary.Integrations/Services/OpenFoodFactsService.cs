using System.Collections.Concurrent;
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
    ILogger<OpenFoodFactsService> logger) : IOpenFoodFactsService {
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StaleSearchCacheTtl = TimeSpan.FromHours(6);
    private static readonly ConcurrentDictionary<string, CachedSearchResult> SearchCache = new(StringComparer.Ordinal);

    public async Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default) {
        try {
            var baseUrl = options.Value.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/api/v2/product/{barcode}?fields=code,product_name,brands,categories,image_url,nutriments";

            var response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OffApiResponse>(cancellationToken: cancellationToken);
            if (result?.Status != 1 || result.Product is null) {
                return null;
            }

            var p = result.Product;
            var name = p.ProductName;
            if (string.IsNullOrWhiteSpace(name)) {
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
            logger.LogWarning(ex, "Open Food Facts lookup failed for barcode '{Barcode}'", barcode);
            return null;
        }
    }

    public async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default) {
        var cacheKey = GetSearchCacheKey(query, limit);
        if (TryGetCachedSearch(cacheKey, SearchCacheTtl, out var freshProducts)) {
            return freshProducts;
        }

        try {
            var baseUrl = options.Value.BaseUrl.TrimEnd('/');
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{baseUrl}/cgi/search.pl?search_terms={encodedQuery}&page_size={limit}&json=1&fields=code,product_name,brands,categories,image_url,nutriments";

            var response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OffSearchResponse>(cancellationToken: cancellationToken);
            if (result?.Products is null) {
                return [];
            }

            var products = result.Products
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
                    p.Nutriments?.Fiber100G))
                .ToList();
            SearchCache[cacheKey] = new CachedSearchResult(DateTimeOffset.UtcNow, products);
            return products;
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) {
            if (TryGetCachedSearch(cacheKey, StaleSearchCacheTtl, out var staleProducts)) {
                logger.LogWarning(ex, "Open Food Facts text search failed for query '{Query}', returning cached result", query);
                return staleProducts;
            }

            logger.LogWarning(ex, "Open Food Facts text search failed for query '{Query}'", query);
            return [];
        }
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetSearchCacheKey(string query, int limit) =>
        $"{query.Trim().ToLowerInvariant()}:{limit}";

    private static bool TryGetCachedSearch(
        string cacheKey,
        TimeSpan maxAge,
        out IReadOnlyList<OpenFoodFactsProductModel> products) {
        if (SearchCache.TryGetValue(cacheKey, out var cached) &&
            DateTimeOffset.UtcNow - cached.CachedAt <= maxAge) {
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
