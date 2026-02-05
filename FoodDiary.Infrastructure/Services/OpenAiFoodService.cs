using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Ai;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Services;

public sealed class OpenAiFoodService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiFoodService> logger,
    IAiUsageRepository aiUsageRepository)
    : IOpenAiFoodService
{
    private readonly OpenAiOptions _options = options.Value;
    private readonly ILogger<OpenAiFoodService> _logger = logger;
    private readonly IAiUsageRepository _aiUsageRepository = aiUsageRepository;

    public async Task<Result<FoodVisionResponse>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result.Failure<FoodVisionResponse>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        var requestModel = _options.VisionModel;
        var request = BuildVisionRequest(requestModel, imageUrl, userLanguage);
        var response = await SendRequestAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            requestModel = _options.VisionFallbackModel;
            var fallback = BuildVisionRequest(requestModel, imageUrl, userLanguage);
            response = await SendRequestAsync(fallback, cancellationToken);
        }

        if (!response.IsSuccess)
        {
            return Result.Failure<FoodVisionResponse>(response.Error);
        }

        var parsed = ParseVisionResponse(response.Json!);
        if (parsed.IsSuccess)
        {
            await SaveUsageAsync(response.Json!, userId, "vision", requestModel, cancellationToken);
        }
        return parsed;
    }

    public async Task<Result<FoodNutritionResponse>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItem> items,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result.Failure<FoodNutritionResponse>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        var requestModel = _options.TextModel;
        var request = BuildNutritionRequest(requestModel, items);
        var response = await SendRequestAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            return Result.Failure<FoodNutritionResponse>(response.Error);
        }

        var parsed = ParseNutritionResponse(response.Json!);
        if (parsed.IsSuccess)
        {
            await SaveUsageAsync(response.Json!, userId, "nutrition", requestModel, cancellationToken);
        }

        return parsed;
    }

    private async Task<(bool IsSuccess, JsonDocument? Json, Error Error)> SendRequestAsync(
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var requestId = response.Headers.TryGetValues("x-request-id", out var values)
                ? string.Join(",", values)
                : null;

            _logger.LogWarning(
                "OpenAI request failed. Status={Status} RequestId={RequestId} Body={Body}",
                statusCode,
                requestId ?? "n/a",
                responseBody);

            return (false, null, Errors.Ai.OpenAiFailed($"OpenAI error {response.StatusCode}: {responseBody}"));
        }

        try
        {
            var json = JsonDocument.Parse(responseBody);
            return (true, json, Error.None);
        }
        catch (JsonException ex)
        {
            return (false, null, Errors.Ai.InvalidResponse($"Invalid JSON response: {ex.Message}"));
        }
    }

    private static object BuildVisionRequest(string model, string imageUrl, string? userLanguage)
    {
        var language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        var includeLocal = language != "en";
        var languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";

        return new
        {
            model,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = "Analyze the food photo and return only JSON with list of items. " +
                                   "Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). " +
                                   "Use grams (g) when possible. " +
                                   languageHint
                        },
                        new
                        {
                            type = "input_image",
                            image_url = imageUrl,
                            detail = "high"
                        }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "food_vision",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            items = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        nameEn = new { type = "string" },
                                        nameLocal = new { type = new[] { "string", "null" } },
                                        amount = new { type = "number" },
                                        unit = new { type = "string" },
                                        confidence = new { type = "number" }
                                    },
                                    required = new[] { "nameEn", "nameLocal", "amount", "unit", "confidence" },
                                    additionalProperties = false
                                }
                            },
                        },
                        required = new[] { "items" },
                        additionalProperties = false
                    },
                    strict = true
                }
            }
        };
    }

    private static object BuildNutritionRequest(string model, IReadOnlyList<FoodVisionItem> items)
    {
        var mappedItems = items.Select(item => new
        {
            name = string.IsNullOrWhiteSpace(item.NameEn) ? (item.NameLocal ?? "unknown") : item.NameEn,
            amount = item.Amount,
            unit = item.Unit
        });

        return new
        {
            model,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = "You are a nutrition assistant. Using the provided items with amounts, " +
                                   "estimate calories and nutrients per item and totals. " +
                                   "Item names are in English. Return only JSON."
                        },
                        new
                        {
                            type = "input_text",
                            text = JsonSerializer.Serialize(new { items = mappedItems })
                        }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "food_nutrition",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            calories = new { type = "number" },
                            protein = new { type = "number" },
                            fat = new { type = "number" },
                            carbs = new { type = "number" },
                            fiber = new { type = "number" },
                            alcohol = new { type = "number" },
                            items = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        name = new { type = "string" },
                                        amount = new { type = "number" },
                                        unit = new { type = "string" },
                                        calories = new { type = "number" },
                                        protein = new { type = "number" },
                                        fat = new { type = "number" },
                                        carbs = new { type = "number" },
                                        fiber = new { type = "number" },
                                        alcohol = new { type = "number" }
                                    },
                                    required = new[]
                                    {
                                        "name", "amount", "unit",
                                        "calories", "protein", "fat", "carbs", "fiber", "alcohol"
                                    },
                                    additionalProperties = false
                                }
                            },
                        },
                        required = new[]
                        {
                            "calories", "protein", "fat", "carbs", "fiber", "alcohol", "items"
                        },
                        additionalProperties = false
                    },
                    strict = true
                }
            }
        };
    }

    private static Result<FoodVisionResponse> ParseVisionResponse(JsonDocument json)
    {
        var text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<FoodVisionResponse>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<FoodVisionResponse>(text, JsonOptions());
            if (parsed is null || parsed.Items is null)
            {
                return Result.Failure<FoodVisionResponse>(Errors.Ai.InvalidResponse("Vision response is empty."));
            }

            return Result.Success(parsed);
        }
        catch (JsonException ex)
        {
            return Result.Failure<FoodVisionResponse>(Errors.Ai.InvalidResponse($"Vision JSON invalid: {ex.Message}"));
        }
    }

    private static Result<FoodNutritionResponse> ParseNutritionResponse(JsonDocument json)
    {
        var text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<FoodNutritionResponse>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<FoodNutritionResponse>(text, JsonOptions());
            if (parsed is null || parsed.Items is null)
            {
                return Result.Failure<FoodNutritionResponse>(Errors.Ai.InvalidResponse("Nutrition response is empty."));
            }

            return Result.Success(parsed);
        }
        catch (JsonException ex)
        {
            return Result.Failure<FoodNutritionResponse>(Errors.Ai.InvalidResponse($"Nutrition JSON invalid: {ex.Message}"));
        }
    }

    private static string? ExtractOutputText(JsonDocument json)
    {
        if (!json.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) &&
                    type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static JsonSerializerOptions JsonOptions()
        => new() { PropertyNameCaseInsensitive = true };

    private async Task SaveUsageAsync(
        JsonDocument json,
        UserId userId,
        string operation,
        string model,
        CancellationToken cancellationToken)
    {
        var usage = ExtractUsage(json);
        if (usage is null)
        {
            return;
        }

        var entity = AiUsage.Create(
            userId,
            operation,
            model,
            usage.Value.InputTokens,
            usage.Value.OutputTokens,
            usage.Value.TotalTokens);

        await _aiUsageRepository.AddAsync(entity, cancellationToken);
    }

    private static UsageTokens? ExtractUsage(JsonDocument json)
    {
        if (!json.RootElement.TryGetProperty("usage", out var usage) ||
            usage.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var input = usage.TryGetProperty("input_tokens", out var inputTokens) ? inputTokens.GetInt32() : 0;
        var output = usage.TryGetProperty("output_tokens", out var outputTokens) ? outputTokens.GetInt32() : 0;
        var total = usage.TryGetProperty("total_tokens", out var totalTokens) ? totalTokens.GetInt32() : input + output;

        return new UsageTokens(input, output, total);
    }

    private readonly record struct UsageTokens(int InputTokens, int OutputTokens, int TotalTokens);
}
