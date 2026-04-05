using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Models;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

internal sealed class UsdaFoodSearchService(
    HttpClient httpClient,
    IOptions<UsdaApiOptions> options,
    ILogger<UsdaFoodSearchService> logger) : IUsdaFoodSearchService {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ApiKey)) {
            logger.LogDebug("USDA API key not configured, skipping branded food search");
            return [];
        }

        try {
            var requestBody = new UsdaSearchRequest(
                query,
                ["Branded"],
                limit);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/foods/search?api_key={config.ApiKey}") {
                Content = JsonContent.Create(requestBody, options: JsonOptions),
            };

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UsdaSearchResponse>(JsonOptions, cancellationToken);
            if (result?.Foods is null) {
                return [];
            }

            return result.Foods
                .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.BrandName ?? f.FoodCategory))
                .ToList();
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "USDA branded food search failed for query '{Query}'", query);
            return [];
        }
    }

    private sealed record UsdaSearchRequest(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("dataType")] string[] DataType,
        [property: JsonPropertyName("pageSize")] int PageSize);

    private sealed class UsdaSearchResponse {
        [JsonPropertyName("foods")]
        public List<UsdaFoodItem>? Foods { get; init; }
    }

    private sealed class UsdaFoodItem {
        [JsonPropertyName("fdcId")]
        public int FdcId { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("foodCategory")]
        public string? FoodCategory { get; init; }

        [JsonPropertyName("brandName")]
        public string? BrandName { get; init; }
    }
}
