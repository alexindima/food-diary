using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

public sealed class OpenAiFoodService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiFoodService> logger,
    IAiUsageRepository aiUsageRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider)
    : IOpenAiFoodService {
    private const int MaxTransientRetries = 2;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(750)
    ];

    private readonly OpenAiOptions _options = options.Value;
    private readonly ILogger<OpenAiFoodService> _logger = logger;
    private readonly IAiUsageRepository _aiUsageRepository = aiUsageRepository;
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        CancellationToken cancellationToken) {
        const string operation = "vision";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        var requestModel = _options.VisionModel;
        var promptTemplate = await aiPromptProvider.GetPromptAsync("vision", cancellationToken);
        var request = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate);
        var response = await SendRequestAsync(request, operation, requestModel, cancellationToken);
        if (!response.IsSuccess) {
            InfrastructureTelemetry.AiFallbackCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
                new KeyValuePair<string, object?>("fooddiary.ai.from_model", requestModel),
                new KeyValuePair<string, object?>("fooddiary.ai.to_model", _options.VisionFallbackModel));
            requestModel = _options.VisionFallbackModel;
            var fallback = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate);
            response = await SendRequestAsync(fallback, operation, requestModel, cancellationToken);
        }

        if (!response.IsSuccess) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        var parsed = ParseVisionResponse(response.Json!);
        if (parsed.IsSuccess) {
            await SaveUsageAsync(response.Json!, userId, "vision", requestModel, cancellationToken);
        }

        return parsed;
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        var requestModel = _options.TextModel;
        var textPrompt = await aiPromptProvider.GetPromptAsync("text-parse", cancellationToken);
        var request = BuildTextParseRequest(requestModel, text, userLanguage, textPrompt);
        var response = await SendRequestAsync(request, operation, requestModel, cancellationToken);
        if (!response.IsSuccess) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        var parsed = ParseVisionResponse(response.Json!);
        if (parsed.IsSuccess) {
            await SaveUsageAsync(response.Json!, userId, "text-parse", requestModel, cancellationToken);
        }

        return parsed;
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodNutritionModel>(quotaCheck.Error);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        var requestModel = _options.TextModel;
        var nutritionPrompt = await aiPromptProvider.GetPromptAsync("nutrition", cancellationToken);
        var request = BuildNutritionRequest(requestModel, items, nutritionPrompt);
        var response = await SendRequestAsync(request, operation, requestModel, cancellationToken);
        if (!response.IsSuccess) {
            return Result.Failure<FoodNutritionModel>(response.Error);
        }

        var parsed = ParseNutritionResponse(response.Json!);
        if (parsed.IsSuccess) {
            await SaveUsageAsync(response.Json!, userId, "nutrition", requestModel, cancellationToken);
        }

        return parsed;
    }

    private async Task<(bool IsSuccess, JsonDocument? Json, Error Error)> SendRequestAsync(
        object payload,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        var requestBody = JsonSerializer.Serialize(payload);

        for (var attempt = 0; attempt <= MaxTransientRetries; attempt++) {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(request, cancellationToken);
            } catch (HttpRequestException ex) when (attempt < MaxTransientRetries) {
                _logger.LogWarning(ex, "OpenAI transport error on attempt {Attempt}. Retrying.", attempt + 1);
                await Task.Delay(RetryDelays[attempt], cancellationToken);
                continue;
            } catch (HttpRequestException ex) {
                _logger.LogWarning(ex, "OpenAI request failed due to transport error.");
                RecordAiRequest(operation, model, "transport_error");
                return (false, null, Errors.Ai.OpenAiFailed($"OpenAI transport error: {ex.Message}"));
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxTransientRetries) {
                _logger.LogWarning(ex, "OpenAI request timed out on attempt {Attempt}. Retrying.", attempt + 1);
                await Task.Delay(RetryDelays[attempt], cancellationToken);
                continue;
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                _logger.LogWarning(ex, "OpenAI request timed out.");
                RecordAiRequest(operation, model, "timeout");
                return (false, null, Errors.Ai.OpenAiFailed("OpenAI request timed out."));
            }

            using var _ = response;
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var statusCode = (int)response.StatusCode;
                var requestId = response.Headers.TryGetValues("x-request-id", out var values)
                    ? string.Join(",", values)
                    : null;

                if (attempt < MaxTransientRetries && IsTransientStatusCode(response.StatusCode)) {
                    _logger.LogWarning(
                        "OpenAI transient failure on attempt {Attempt}. Status={Status} RequestId={RequestId} Summary={Summary}. Retrying.",
                        attempt + 1,
                        statusCode,
                        requestId ?? "n/a",
                        SummarizeErrorBody(responseBody));
                    await Task.Delay(RetryDelays[attempt], cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "OpenAI request failed. Status={Status} RequestId={RequestId} Summary={Summary}",
                    statusCode,
                    requestId ?? "n/a",
                    SummarizeErrorBody(responseBody));

                RecordAiRequest(operation, model, $"http_{statusCode}");
                return (false, null, Errors.Ai.OpenAiFailed($"OpenAI error {response.StatusCode}: {SummarizeErrorBody(responseBody)}"));
            }

            try {
                var json = JsonDocument.Parse(responseBody);
                RecordAiRequest(operation, model, "success");
                return (true, json, Error.None);
            } catch (JsonException ex) {
                RecordAiRequest(operation, model, "invalid_json");
                return (false, null, Errors.Ai.InvalidResponse($"Invalid JSON response: {ex.Message}"));
            }
        }

        RecordAiRequest(operation, model, "retry_exhausted");
        return (false, null, Errors.Ai.OpenAiFailed("OpenAI request failed after retries."));
    }

    private static object BuildVisionRequest(
        string model,
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate) {
        var language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        var includeLocal = language != "en";
        var languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";
        var descriptionHint = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"User hint: {description.Trim()}. ";

        var resolvedPrompt = promptTemplate
            .Replace("{{languageHint}}", languageHint)
            .Replace("{{descriptionHint}}", descriptionHint);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = descriptionHint + resolvedPrompt
                        },
                        new {
                            type = "input_image",
                            image_url = imageUrl,
                            detail = "high"
                        }
                    }
                }
            },
            text = new {
                format = new {
                    type = "json_schema",
                    name = "food_vision",
                    schema = new {
                        type = "object",
                        properties = new {
                            items = new {
                                type = "array",
                                items = new {
                                    type = "object",
                                    properties = new {
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

    private static object BuildTextParseRequest(string model, string text, string? userLanguage, string promptTemplate) {
        var language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        var includeLocal = language != "en";
        var languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";

        var resolvedPrompt = promptTemplate
            .Replace("{{userText}}", text)
            .Replace("{{languageHint}}", languageHint);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt
                        }
                    }
                }
            },
            text = new {
                format = new {
                    type = "json_schema",
                    name = "food_vision",
                    schema = new {
                        type = "object",
                        properties = new {
                            items = new {
                                type = "array",
                                items = new {
                                    type = "object",
                                    properties = new {
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

    private static object BuildNutritionRequest(string model, IReadOnlyList<FoodVisionItemModel> items, string promptTemplate) {
        var mappedItems = items.Select(item => new {
            name = string.IsNullOrWhiteSpace(item.NameEn) ? (item.NameLocal ?? "unknown") : item.NameEn,
            amount = item.Amount,
            unit = item.Unit
        });

        var itemsJson = JsonSerializer.Serialize(new { items = mappedItems });
        var resolvedPrompt = promptTemplate
            .Replace("{{itemsJson}}", itemsJson);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt
                        },
                        new {
                            type = "input_text",
                            text = itemsJson
                        }
                    }
                }
            },
            text = new {
                format = new {
                    type = "json_schema",
                    name = "food_nutrition",
                    schema = new {
                        type = "object",
                        properties = new {
                            calories = new { type = "number" },
                            protein = new { type = "number" },
                            fat = new { type = "number" },
                            carbs = new { type = "number" },
                            fiber = new { type = "number" },
                            alcohol = new { type = "number" },
                            items = new {
                                type = "array",
                                items = new {
                                    type = "object",
                                    properties = new {
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
                                    required = new[] {
                                        "name", "amount", "unit",
                                        "calories", "protein", "fat", "carbs", "fiber", "alcohol"
                                    },
                                    additionalProperties = false
                                }
                            },
                        },
                        required = new[] {
                            "calories", "protein", "fat", "carbs", "fiber", "alcohol", "items"
                        },
                        additionalProperties = false
                    },
                    strict = true
                }
            }
        };
    }

    private static Result<FoodVisionModel> ParseVisionResponse(JsonDocument json) {
        var text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try {
            var parsed = JsonSerializer.Deserialize<FoodVisionModel>(text, JsonOptions());
            if (parsed is null || parsed.Items is null) {
                return Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse("Vision response is empty."));
            }

            return Result.Success(parsed);
        } catch (JsonException ex) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse($"Vision JSON invalid: {ex.Message}"));
        }
    }

    private static Result<FoodNutritionModel> ParseNutritionResponse(JsonDocument json) {
        var text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try {
            var parsed = JsonSerializer.Deserialize<FoodNutritionModel>(text, JsonOptions());
            if (parsed is null || parsed.Items is null) {
                return Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse("Nutrition response is empty."));
            }

            return Result.Success(parsed);
        } catch (JsonException ex) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse($"Nutrition JSON invalid: {ex.Message}"));
        }
    }

    private static string? ExtractOutputText(JsonDocument json) {
        if (!json.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) {
            return null;
        }

        foreach (var item in output.EnumerateArray()) {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) {
                continue;
            }

            foreach (var part in content.EnumerateArray()) {
                if (part.TryGetProperty("type", out var type) &&
                    type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text)) {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static JsonSerializerOptions JsonOptions()
        => new() { PropertyNameCaseInsensitive = true };

    private async Task<Result> EnsureMonthlyQuotaAsync(UserId userId, string operation, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(userId.Value));
        }

        var nowUtc = dateTimeProvider.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndUtc = monthStartUtc.AddMonths(1);
        var totals = await _aiUsageRepository.GetUserTotalsAsync(userId, monthStartUtc, monthEndUtc, cancellationToken);

        if (totals.InputTokens >= user.AiInputTokenLimit || totals.OutputTokens >= user.AiOutputTokenLimit) {
            InfrastructureTelemetry.AiQuotaRejectionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.ai.operation", operation));
            return Result.Failure(Errors.Ai.QuotaExceeded());
        }

        return Result.Success();
    }

    private static void RecordAiRequest(string operation, string model, string outcome) {
        InfrastructureTelemetry.AiRequestCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.ai.model", model),
            new KeyValuePair<string, object?>("fooddiary.ai.outcome", outcome));
    }

    private async Task SaveUsageAsync(
        JsonDocument json,
        UserId userId,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        var usage = ExtractUsage(json);
        if (usage is null) {
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

    private static UsageTokens? ExtractUsage(JsonDocument json) {
        if (!json.RootElement.TryGetProperty("usage", out var usage) ||
            usage.ValueKind != JsonValueKind.Object) {
            return null;
        }

        var input = usage.TryGetProperty("input_tokens", out var inputTokens) ? inputTokens.GetInt32() : 0;
        var output = usage.TryGetProperty("output_tokens", out var outputTokens) ? outputTokens.GetInt32() : 0;
        var total = usage.TryGetProperty("total_tokens", out var totalTokens) ? totalTokens.GetInt32() : input + output;

        return new UsageTokens(input, output, total);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) {
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
               HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or
               HttpStatusCode.InternalServerError;
    }

    private static string SummarizeErrorBody(string responseBody) {
        if (string.IsNullOrWhiteSpace(responseBody)) {
            return "empty";
        }

        try {
            var root = JsonNode.Parse(responseBody);
            var errorNode = root?["error"];
            if (errorNode is JsonValue errorValue && errorValue.TryGetValue<string>(out var errorText)) {
                return TrimSummary(errorText);
            }

            if (errorNode is JsonObject errorObject) {
                var parts = new List<string>();
                if (errorObject["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var errorType) &&
                    !string.IsNullOrWhiteSpace(errorType)) {
                    parts.Add(errorType.Trim());
                }

                if (errorObject["code"] is JsonValue codeValue && codeValue.TryGetValue<string>(out var errorCode) &&
                    !string.IsNullOrWhiteSpace(errorCode)) {
                    parts.Add(errorCode.Trim());
                }

                if (errorObject["message"] is JsonValue messageValue &&
                    messageValue.TryGetValue<string>(out var errorMessage) &&
                    !string.IsNullOrWhiteSpace(errorMessage)) {
                    parts.Add(errorMessage.Trim());
                }

                if (parts.Count > 0) {
                    return TrimSummary(string.Join(": ", parts));
                }
            }
        } catch (JsonException) {
        }

        return "response body unavailable";
    }

    private static string TrimSummary(string value) {
        var compact = value.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 200 ? compact : compact[..200];
    }

    private readonly record struct UsageTokens(int InputTokens, int OutputTokens, int TotalTokens);
}
