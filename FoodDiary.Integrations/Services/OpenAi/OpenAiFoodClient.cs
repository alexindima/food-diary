using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services.OpenAi;

public sealed class OpenAiFoodClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiFoodClient> logger)
    : IOpenAiFoodClient {
    private const int MaxTransientRetries = 2;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(750),
    ];

    private readonly OpenAiOptions _options = options.Value;

    public async Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate,
        CancellationToken cancellationToken) {
        const string operation = "vision";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        string requestModel = _options.VisionModel;
        object request = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate);
        (bool IsSuccess, JsonDocument? Json, Error Error) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) {
            IntegrationsTelemetry.AiFallbackCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
                new KeyValuePair<string, object?>("fooddiary.ai.from_model", requestModel),
                new KeyValuePair<string, object?>("fooddiary.ai.to_model", _options.VisionFallbackModel));
            requestModel = _options.VisionFallbackModel;
            object fallback = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate);
            response = await SendRequestAsync(fallback, operation, requestModel, cancellationToken).ConfigureAwait(false);
        }

        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(response.Error);
        }

        Result<FoodVisionModel> parsed = ParseVisionResponse(response.Json!);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(response.Json!)));
    }

    public async Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        string promptTemplate,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        string requestModel = _options.TextModel;
        object request = BuildTextParseRequest(requestModel, text, userLanguage, promptTemplate);
        (bool IsSuccess, JsonDocument? Json, Error Error) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(response.Error);
        }

        Result<FoodVisionModel> parsed = ParseVisionResponse(response.Json!);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(response.Json!)));
    }

    public async Task<Result<OpenAiFoodClientResponse<FoodNutritionModel>>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        string promptTemplate,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        string requestModel = _options.TextModel;
        object request = BuildNutritionRequest(requestModel, items, promptTemplate);
        (bool IsSuccess, JsonDocument? Json, Error Error) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(response.Error);
        }

        Result<FoodNutritionModel> parsed = ParseNutritionResponse(response.Json!);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(response.Json!)));
    }

    private async Task<(bool IsSuccess, JsonDocument? Json, Error Error)> SendRequestAsync(
        object payload,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        string requestBody = JsonSerializer.Serialize(payload);

        for (int attempt = 0; attempt <= MaxTransientRetries; attempt++) {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            } catch (HttpRequestException ex) when (attempt < MaxTransientRetries) {
                logger.LogWarning(ex, "OpenAI transport error on attempt {Attempt}. Retrying.", attempt + 1);
                await Task.Delay(RetryDelays[attempt], cancellationToken).ConfigureAwait(false);
                continue;
            } catch (HttpRequestException ex) {
                logger.LogWarning(ex, "OpenAI request failed due to transport error.");
                RecordAiRequest(operation, model, "transport_error");
                return (false, null, Errors.Ai.OpenAiFailed($"OpenAI transport error: {ex.Message}"));
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxTransientRetries) {
                logger.LogWarning(ex, "OpenAI request timed out on attempt {Attempt}. Retrying.", attempt + 1);
                await Task.Delay(RetryDelays[attempt], cancellationToken).ConfigureAwait(false);
                continue;
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                logger.LogWarning(ex, "OpenAI request timed out.");
                RecordAiRequest(operation, model, "timeout");
                return (false, null, Errors.Ai.OpenAiFailed("OpenAI request timed out."));
            }

            using HttpResponseMessage _ = response;
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) {
                (bool ShouldRetry, Error Error) failedResponse = await HandleFailedResponseAsync(response, responseBody, attempt, operation, model, cancellationToken).ConfigureAwait(false);
                if (failedResponse.ShouldRetry) {
                    continue;
                }

                return (false, null, failedResponse.Error);
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

    private async Task<(bool ShouldRetry, Error Error)> HandleFailedResponseAsync(
        HttpResponseMessage response,
        string responseBody,
        int attempt,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        int statusCode = (int)response.StatusCode;
        string? requestId = response.Headers.TryGetValues("x-request-id", out IEnumerable<string>? values)
            ? string.Join(',', values)
            : null;
        string summary = SummarizeErrorBody(responseBody);

        if (IsTransientStatusCode(response.StatusCode)) {
            if (attempt < MaxTransientRetries) {
                logger.LogWarning(
                    "OpenAI transient failure on attempt {Attempt}. Status={Status} RequestId={RequestId} Summary={Summary}. Retrying.",
                    attempt + 1,
                    statusCode,
                    requestId ?? "n/a",
                    summary);
                await Task.Delay(RetryDelays[attempt], cancellationToken).ConfigureAwait(false);
            } else {
                logger.LogWarning(
                    "OpenAI transient failure exhausted retries. Status={Status} RequestId={RequestId} Summary={Summary}",
                    statusCode,
                    requestId ?? "n/a",
                    summary);
            }

            return (true, Error.None);
        }

        logger.LogWarning(
            "OpenAI request failed. Status={Status} RequestId={RequestId} Summary={Summary}",
            statusCode,
            requestId ?? "n/a",
            summary);

        RecordAiRequest(operation, model, string.Create(CultureInfo.InvariantCulture, $"http_{statusCode}"));
        return (false, Errors.Ai.OpenAiFailed($"OpenAI error {response.StatusCode}: {summary}"));
    }

    private static object BuildVisionRequest(
        string model,
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate) {
        string language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        bool includeLocal = !string.Equals(language, "en", StringComparison.Ordinal);
        string languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";
        string descriptionHint = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"User hint: {description.Trim()}. ";

        string resolvedPrompt = promptTemplate
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal)
            .Replace("{{descriptionHint}}", descriptionHint, StringComparison.Ordinal);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = descriptionHint + resolvedPrompt,
                        },
                        new {
                            type = "input_image",
                            image_url = imageUrl,
                            detail = "high",
                        },
                    },
                },
            },
            text = BuildFoodVisionTextFormat(),
        };
    }

    private static object BuildTextParseRequest(string model, string text, string? userLanguage, string promptTemplate) {
        string language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        bool includeLocal = !string.Equals(language, "en", StringComparison.Ordinal);
        string languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";

        string resolvedPrompt = promptTemplate
            .Replace("{{userText}}", text, StringComparison.Ordinal)
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt,
                        },
                    },
                },
            },
            text = BuildFoodVisionTextFormat(),
        };
    }

    private static object BuildFoodVisionTextFormat() {
        return new {
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
                                    confidence = new { type = "number" },
                                },
                                required = new[] { "nameEn", "nameLocal", "amount", "unit", "confidence" },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] { "items" },
                    additionalProperties = false,
                },
                strict = true,
            },
        };
    }

    private static object BuildNutritionRequest(string model, IReadOnlyList<FoodVisionItemModel> items, string promptTemplate) {
        var mappedItems = items.Select(item => new {
            name = string.IsNullOrWhiteSpace(item.NameEn) ? (item.NameLocal ?? "unknown") : item.NameEn,
            amount = item.Amount,
            unit = item.Unit,
        });

        string itemsJson = JsonSerializer.Serialize(new { items = mappedItems });
        string resolvedPrompt = promptTemplate
            .Replace("{{itemsJson}}", itemsJson, StringComparison.Ordinal);

        return new {
            model,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt,
                        },
                        new {
                            type = "input_text",
                            text = itemsJson,
                        },
                    },
                },
            },
            text = BuildFoodNutritionTextFormat(),
        };
    }

    private static object BuildFoodNutritionTextFormat() {
        return new {
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
                                    alcohol = new { type = "number" },
                                },
                                required = new[] {
                                    "name", "amount", "unit",
                                    "calories", "protein", "fat", "carbs", "fiber", "alcohol",
                                },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] {
                        "calories", "protein", "fat", "carbs", "fiber", "alcohol", "items",
                    },
                    additionalProperties = false,
                },
                strict = true,
            },
        };
    }

    private static Result<FoodVisionModel> ParseVisionResponse(JsonDocument json) {
        string? text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try {
            FoodVisionModel? parsed = JsonSerializer.Deserialize<FoodVisionModel>(text, JsonOptions());
            return parsed is null ? Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse("Vision response is empty.")) : Result.Success(parsed);
        } catch (JsonException ex) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.InvalidResponse($"Vision JSON invalid: {ex.Message}"));
        }
    }

    private static Result<FoodNutritionModel> ParseNutritionResponse(JsonDocument json) {
        string? text = ExtractOutputText(json);
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse("Missing output text."));
        }

        try {
            FoodNutritionModel? parsed = JsonSerializer.Deserialize<FoodNutritionModel>(text, JsonOptions());
            return parsed is null ? Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse("Nutrition response is empty.")) : Result.Success(parsed);
        } catch (JsonException ex) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.InvalidResponse($"Nutrition JSON invalid: {ex.Message}"));
        }
    }

    private static string? ExtractOutputText(JsonDocument json) {
        if (!json.RootElement.TryGetProperty("output", out JsonElement output) || output.ValueKind != JsonValueKind.Array) {
            return null;
        }

        foreach (JsonElement item in output.EnumerateArray()) {
            if (!item.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Array) {
                continue;
            }

            foreach (JsonElement part in content.EnumerateArray()) {
                if (part.TryGetProperty("type", out JsonElement type) &&
string.Equals(type.GetString(), "output_text", StringComparison.Ordinal) &&
                    part.TryGetProperty("text", out JsonElement text)) {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static JsonSerializerOptions JsonOptions()
        => new() { PropertyNameCaseInsensitive = true };

    private static void RecordAiRequest(string operation, string model, string outcome) {
        IntegrationsTelemetry.AiRequestCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.ai.model", model),
            new KeyValuePair<string, object?>("fooddiary.ai.outcome", outcome));
    }

    private static AiUsageTokens? ExtractUsage(JsonDocument json) {
        if (!json.RootElement.TryGetProperty("usage", out JsonElement usage) ||
            usage.ValueKind != JsonValueKind.Object) {
            return null;
        }

        int input = usage.TryGetProperty("input_tokens", out JsonElement inputTokens) ? inputTokens.GetInt32() : 0;
        int output = usage.TryGetProperty("output_tokens", out JsonElement outputTokens) ? outputTokens.GetInt32() : 0;
        int total = usage.TryGetProperty("total_tokens", out JsonElement totalTokens) ? totalTokens.GetInt32() : input + output;

        return new AiUsageTokens(input, output, total);
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
            if (root != null) {
                switch (root["error"]) {
                    case JsonValue errorValue when errorValue.TryGetValue(out string? errorText):
                        return TrimSummary(errorText);
                    case JsonObject errorObject: {
                            var parts = new List<string>();
                            if (errorObject["type"] is JsonValue typeValue && typeValue.TryGetValue(out string? errorType) &&
                                !string.IsNullOrWhiteSpace(errorType)) {
                                parts.Add(errorType.Trim());
                            }

                            if (errorObject["code"] is JsonValue codeValue && codeValue.TryGetValue(out string? errorCode) &&
                                !string.IsNullOrWhiteSpace(errorCode)) {
                                parts.Add(errorCode.Trim());
                            }

                            if (errorObject["message"] is JsonValue messageValue &&
                                messageValue.TryGetValue(out string? errorMessage) &&
                                !string.IsNullOrWhiteSpace(errorMessage)) {
                                parts.Add(errorMessage.Trim());
                            }

                            if (parts.Count > 0) {
                                return TrimSummary(string.Join(": ", parts));
                            }

                            break;
                        }
                }
            }
        } catch (JsonException) {
        }

        return "response body unavailable";
    }

    private static string TrimSummary(string value) {
        string compact = value.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 200 ? compact : compact[..200];
    }
}
