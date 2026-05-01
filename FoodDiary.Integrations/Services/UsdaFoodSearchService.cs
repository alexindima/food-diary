using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services;

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

    public async Task<UsdaFoodDetailModel?> GetFoodDetailAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ApiKey)) {
            logger.LogDebug("USDA API key not configured, skipping food detail lookup");
            return null;
        }

        try {
            var response = await httpClient.GetAsync($"{config.BaseUrl}/food/{fdcId}?api_key={config.ApiKey}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var food = await response.Content.ReadFromJsonAsync<UsdaFoodDetailResponse>(JsonOptions, cancellationToken);
            if (food is null) {
                return null;
            }

            var nutrients = food.FoodNutrients
                .Where(n => n.Nutrient is not null && n.Amount.HasValue)
                .Select(n => new MicronutrientModel(
                    n.Nutrient!.Id,
                    n.Nutrient.Name,
                    n.Nutrient.UnitName,
                    n.Amount!.Value,
                    null,
                    null))
                .ToList();

            var portions = food.FoodPortions
                .Select((p, index) => new UsdaFoodPortionModel(
                    p.Id ?? index + 1,
                    p.Amount ?? 1,
                    p.MeasureUnit?.Name ?? p.Modifier ?? "serving",
                    p.GramWeight ?? 0,
                    p.PortionDescription,
                    p.Modifier))
                .ToList();

            return new UsdaFoodDetailModel(
                food.FdcId,
                food.Description,
                food.FoodCategory?.Description ?? food.FoodCategoryDescription ?? food.BrandName,
                nutrients,
                portions,
                null);
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "USDA food detail lookup failed for FDC ID {FdcId}", fdcId);
            return null;
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

    private sealed class UsdaFoodDetailResponse {
        [JsonPropertyName("fdcId")]
        public int FdcId { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("brandName")]
        public string? BrandName { get; init; }

        [JsonPropertyName("foodCategory")]
        public UsdaFoodCategory? FoodCategory { get; init; }

        [JsonPropertyName("foodCategoryDescription")]
        public string? FoodCategoryDescription { get; init; }

        [JsonPropertyName("foodNutrients")]
        public List<UsdaFoodNutrientItem> FoodNutrients { get; init; } = [];

        [JsonPropertyName("foodPortions")]
        public List<UsdaFoodPortionItem> FoodPortions { get; init; } = [];
    }

    private sealed class UsdaFoodCategory {
        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }

    private sealed class UsdaFoodNutrientItem {
        [JsonPropertyName("amount")]
        public double? Amount { get; init; }

        [JsonPropertyName("nutrient")]
        public UsdaNutrientItem? Nutrient { get; init; }
    }

    private sealed class UsdaNutrientItem {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("unitName")]
        public string UnitName { get; init; } = string.Empty;
    }

    private sealed class UsdaFoodPortionItem {
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        [JsonPropertyName("amount")]
        public double? Amount { get; init; }

        [JsonPropertyName("gramWeight")]
        public double? GramWeight { get; init; }

        [JsonPropertyName("portionDescription")]
        public string? PortionDescription { get; init; }

        [JsonPropertyName("modifier")]
        public string? Modifier { get; init; }

        [JsonPropertyName("measureUnit")]
        public UsdaMeasureUnit? MeasureUnit { get; init; }
    }

    private sealed class UsdaMeasureUnit {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
