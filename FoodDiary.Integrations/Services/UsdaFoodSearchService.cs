using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services;

internal sealed class UsdaFoodSearchService(
    HttpClient httpClient,
    IOptions<UsdaApiOptions> options,
    UsdaFoodDetailCache detailCache,
    ILogger<UsdaFoodSearchService> logger) : IUsdaFoodSearchService {
    private const int MaximumSearchResults = 200;
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    public async Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        UsdaApiOptions config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ApiKey)) {
            logger.LogDebug("USDA API key not configured, skipping branded food search");
            return [];
        }

        int normalizedLimit = Math.Clamp(limit, 1, MaximumSearchResults);
        try {
            var requestBody = new UsdaSearchRequest(
                query,
                ["Branded"],
                normalizedLimit);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/foods/search?api_key={config.ApiKey}") {
                Content = JsonContent.Create(requestBody, options: JsonOptions),
            };

            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            UsdaSearchResponse? result = await ReadJsonAsync<UsdaSearchResponse>(response.Content, cancellationToken).ConfigureAwait(false);
            if (result?.Foods is null) {
                return [];
            }

            return result.Foods
                .Take(normalizedLimit)
                .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.BrandName ?? f.FoodCategory))
                .ToList();
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException or TimeoutException) {
            logger.LogWarning(ex, "USDA branded food search failed. QueryLength={QueryLength}", query.Length);
            return [];
        }
    }

    public async Task<UsdaFoodDetailModel?> GetFoodDetailAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        UsdaApiOptions config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ApiKey)) {
            logger.LogDebug("USDA API key not configured, skipping food detail lookup");
            return null;
        }

        return await detailCache.GetOrCreateAsync(
            config.BaseUrl,
            fdcId,
            () => GetFoodDetailCoreAsync(config, fdcId, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<UsdaFoodDetailLookupResult> GetFoodDetailCoreAsync(
        UsdaApiOptions config,
        int fdcId,
        CancellationToken cancellationToken) {
        try {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                string.Create(CultureInfo.InvariantCulture, $"{config.BaseUrl}/food/{fdcId}?api_key={config.ApiKey}"));
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return new UsdaFoodDetailLookupResult(Cacheable: true, Value: null);
            }

            response.EnsureSuccessStatusCode();

            UsdaFoodDetailResponse? food = await ReadJsonAsync<UsdaFoodDetailResponse>(response.Content, cancellationToken).ConfigureAwait(false);
            if (food is null) {
                return new UsdaFoodDetailLookupResult(Cacheable: false, Value: null);
            }

            var nutrients = food.FoodNutrients
                .Where(n => n.Nutrient is not null && n.Amount.HasValue)
                .Select(n => new MicronutrientModel(
                    n.Nutrient!.Id,
                    n.Nutrient.Name,
                    n.Nutrient.UnitName,
                    n.Amount!.Value,
                    DailyValue: null,
                    PercentDailyValue: null))
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

            return new UsdaFoodDetailLookupResult(Cacheable: true, new UsdaFoodDetailModel(
                food.FdcId,
                food.Description,
                food.FoodCategory?.Description ?? food.FoodCategoryDescription ?? food.BrandName,
                nutrients,
                portions,
                HealthScores: null));
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException or TimeoutException) {
            logger.LogWarning(ex, "USDA food detail lookup failed for FDC ID {FdcId}", fdcId);
            return new UsdaFoodDetailLookupResult(Cacheable: false, Value: null);
        }
    }

    private static Task<T?> ReadJsonAsync<T>(HttpContent content, CancellationToken cancellationToken) =>
        BoundedHttpContentReader.ReadFromJsonAsync<T>(
            content,
            JsonOptions,
            BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
            BoundedHttpContentReader.DefaultReadTimeout,
            cancellationToken);

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
