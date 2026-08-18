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
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services.OpenAi;

public sealed class OpenAiFoodClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiFoodClient> logger,
    TimeProvider? timeProvider = null,
    TimeSpan? overallRequestTimeout = null)
    : IOpenAiFoodClient {
    private const int MaxTransientRetries = 2;
    private static readonly TimeSpan DefaultOverallRequestTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(750),
    ];

    private readonly OpenAiOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _overallRequestTimeout = overallRequestTimeout is null or { Ticks: > 0 }
        ? overallRequestTimeout ?? DefaultOverallRequestTimeout
        : throw new ArgumentOutOfRangeException(nameof(overallRequestTimeout));

    public async Task<Result<AiProviderTokenBudget>> GetAnalyzeFoodImageTokenBudgetAsync(
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<AiProviderTokenBudget>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        object primary = BuildVisionRequest(_options.VisionModel, imageUrl, userLanguage, description, promptTemplate, _options.MaxOutputTokens);
        Result<long> primaryCount = await CountInputTokensAsync(primary, cancellationToken).ConfigureAwait(false);
        if (primaryCount.IsFailure) {
            return Result.Failure<AiProviderTokenBudget>(primaryCount.Error);
        }

        long inputTokens = primaryCount.Value;
        if (!string.Equals(_options.VisionFallbackModel, _options.VisionModel, StringComparison.Ordinal)) {
            object fallback = BuildVisionRequest(_options.VisionFallbackModel, imageUrl, userLanguage, description, promptTemplate, _options.MaxOutputTokens);
            Result<long> fallbackCount = await CountInputTokensAsync(fallback, cancellationToken).ConfigureAwait(false);
            if (fallbackCount.IsFailure) {
                return Result.Failure<AiProviderTokenBudget>(fallbackCount.Error);
            }

            inputTokens = Math.Max(inputTokens, fallbackCount.Value);
        }

        return Result.Success(new AiProviderTokenBudget(inputTokens, _options.MaxOutputTokens));
    }

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
        object request = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate, _options.MaxOutputTokens);
        (bool IsSuccess, JsonDocument? Json, Error Error, bool CanFallback) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess && response.CanFallback) {
            IntegrationsTelemetry.AiFallbackCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
                new KeyValuePair<string, object?>("fooddiary.ai.from_model", requestModel),
                new KeyValuePair<string, object?>("fooddiary.ai.to_model", _options.VisionFallbackModel));
            requestModel = _options.VisionFallbackModel;
            object fallback = BuildVisionRequest(requestModel, imageUrl, userLanguage, description, promptTemplate, _options.MaxOutputTokens);
            response = await SendRequestAsync(fallback, operation, requestModel, cancellationToken).ConfigureAwait(false);
        }

        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(response.Error);
        }

        using JsonDocument json = response.Json!;
        Result<FoodVisionModel> parsed = ParseVisionResponse(json);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(json)));
    }

    public async Task<Result<AiProviderTokenBudget>> GetParseFoodTextTokenBudgetAsync(
        string text,
        string? userLanguage,
        string promptTemplate,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<AiProviderTokenBudget>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        object request = BuildTextParseRequest(_options.TextModel, text, userLanguage, promptTemplate, _options.MaxOutputTokens);
        Result<long> count = await CountInputTokensAsync(request, cancellationToken).ConfigureAwait(false);
        return count.IsFailure
            ? Result.Failure<AiProviderTokenBudget>(count.Error)
            : Result.Success(new AiProviderTokenBudget(count.Value, _options.MaxOutputTokens));
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
        object request = BuildTextParseRequest(requestModel, text, userLanguage, promptTemplate, _options.MaxOutputTokens);
        (bool IsSuccess, JsonDocument? Json, Error Error, bool CanFallback) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(response.Error);
        }

        using JsonDocument json = response.Json!;
        Result<FoodVisionModel> parsed = ParseVisionResponse(json);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(json)));
    }

    public async Task<Result<AiProviderTokenBudget>> GetCalculateNutritionTokenBudgetAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        string promptTemplate,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<AiProviderTokenBudget>(Errors.Ai.OpenAiFailed("OpenAI API key is not configured."));
        }

        object request = BuildNutritionRequest(_options.TextModel, items, promptTemplate, _options.MaxOutputTokens);
        Result<long> count = await CountInputTokensAsync(request, cancellationToken).ConfigureAwait(false);
        return count.IsFailure
            ? Result.Failure<AiProviderTokenBudget>(count.Error)
            : Result.Success(new AiProviderTokenBudget(count.Value, _options.MaxOutputTokens));
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
        object request = BuildNutritionRequest(requestModel, items, promptTemplate, _options.MaxOutputTokens);
        (bool IsSuccess, JsonDocument? Json, Error Error, bool CanFallback) response = await SendRequestAsync(request, operation, requestModel, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) {
            return Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(response.Error);
        }

        using JsonDocument json = response.Json!;
        Result<FoodNutritionModel> parsed = ParseNutritionResponse(json);
        if (parsed.IsFailure) {
            return Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(parsed.Error);
        }

        return Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(
            parsed.Value,
            operation,
            requestModel,
            ExtractUsage(json)));
    }

    private async Task<Result<long>> CountInputTokensAsync(object payload, CancellationToken cancellationToken) {
        JsonObject inputTokenPayload = JsonSerializer.SerializeToNode(payload) as JsonObject
            ?? throw new InvalidOperationException("OpenAI input token payload must be a JSON object.");
        inputTokenPayload.Remove("max_output_tokens");
        string requestBody = inputTokenPayload.ToJsonString();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses/input_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
        } catch (HttpRequestException ex) {
            logger.LogWarning(ex, "OpenAI input token count request failed due to transport error.");
            return Result.Failure<long>(Errors.Ai.OpenAiFailed("OpenAI input token count request failed."));
        } catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) {
            logger.LogWarning("OpenAI input token count request timed out.");
            return Result.Failure<long>(Errors.Ai.OpenAiFailed("OpenAI input token count request timed out."));
        }

        using HttpResponseMessage _ = response;
        string responseBody;
        try {
            responseBody = await BoundedHttpContentReader.ReadAsStringAsync(
                response.Content,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
        } catch (Exception exception) when (exception is InvalidDataException or TimeoutException) {
            logger.LogWarning("OpenAI input token count response body was oversized or exceeded its read deadline.");
            return Result.Failure<long>(Errors.Ai.InvalidResponse("OpenAI input token count response was invalid."));
        }
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning(
                "OpenAI input token count request failed. Status={Status} Summary={Summary}",
                (int)response.StatusCode,
                SummarizeErrorBody(responseBody));
            return Result.Failure<long>(Errors.Ai.OpenAiFailed("OpenAI input token count request was rejected."));
        }

        try {
            using var json = JsonDocument.Parse(responseBody, new JsonDocumentOptions {
                MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
            });
            if (json.RootElement.TryGetProperty("input_tokens", out JsonElement tokensElement) &&
                tokensElement.TryGetInt64(out long inputTokens) &&
                inputTokens > 0) {
                return Result.Success(inputTokens);
            }
        } catch (JsonException ex) {
            logger.LogWarning(ex, "OpenAI input token count response was invalid JSON.");
        }

        return Result.Failure<long>(Errors.Ai.InvalidResponse("OpenAI input token count response was invalid."));
    }

    private async Task<(bool IsSuccess, JsonDocument? Json, Error Error, bool CanFallback)> SendRequestAsync(
        object payload,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        string requestBody = JsonSerializer.Serialize(payload);
        using var overallDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overallDeadline.CancelAfter(_overallRequestTimeout);

        for (int attempt = 0; attempt <= MaxTransientRetries; attempt++) {
            using HttpRequestMessage request = CreateResponseRequest(requestBody);

            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    overallDeadline.Token).ConfigureAwait(false);
            } catch (HttpRequestException ex) {
                logger.LogWarning(ex, "OpenAI request failed due to transport error.");
                RecordAiRequest(operation, model, "transport_error");
                return (false, null, Errors.Ai.OpenAiFailed("OpenAI transport error."), false);
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                logger.LogWarning(ex, "OpenAI request timed out.");
                RecordAiRequest(operation, model, "timeout");
                return (false, null, Errors.Ai.OpenAiFailed("OpenAI request timed out."), false);
            }

            using HttpResponseMessage _ = response;
            (bool IsSuccess, string? Body, Error Error) responseBody = await ReadResponseBodyWithinDeadlineAsync(
                response.Content, operation,
                model,
                overallDeadline.Token,
                cancellationToken).ConfigureAwait(false);
            if (!responseBody.IsSuccess) {
                return (false, null, responseBody.Error, false);
            }

            if (!response.IsSuccessStatusCode) {
                (bool ShouldRetry, Error Error) failedResponse;
                try {
                    failedResponse = await HandleFailedResponseAsync(
                        response,
                        responseBody.Body!,
                        attempt,
                        operation,
                        model,
                        overallDeadline.Token).ConfigureAwait(false);
                } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                    RecordAiRequest(operation, model, "timeout");
                    return (false, null, Errors.Ai.OpenAiFailed("OpenAI request deadline expired."), false);
                }
                if (failedResponse.ShouldRetry) {
                    continue;
                }

                return (false, null, failedResponse.Error, true);
            }

            return ParseSuccessfulResponse(responseBody.Body!, operation, model);
        }

        RecordAiRequest(operation, model, "retry_exhausted");
        return (false, null, Errors.Ai.OpenAiFailed("OpenAI request failed after retries."), true);
    }

    private HttpRequestMessage CreateResponseRequest(string requestBody) {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        return request;
    }

    private static async Task<(bool IsSuccess, string? Body, Error Error)> ReadResponseBodyAsync(
        HttpContent content,
        string operation,
        string model,
        CancellationToken cancellationToken) {
        try {
            string body = await BoundedHttpContentReader.ReadAsStringAsync(
                content,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
            return (true, body, Error.None);
        } catch (InvalidDataException) {
            RecordAiRequest(operation, model, "oversized_response");
            return (false, null, Errors.Ai.InvalidResponse("OpenAI response exceeded the configured size limit."));
        } catch (TimeoutException) {
            RecordAiRequest(operation, model, "response_body_timeout");
            return (false, null, Errors.Ai.InvalidResponse("OpenAI response body exceeded its read deadline."));
        }
    }

    private static async Task<(bool IsSuccess, string? Body, Error Error)> ReadResponseBodyWithinDeadlineAsync(
        HttpContent content,
        string operation,
        string model,
        CancellationToken deadlineToken,
        CancellationToken callerToken) {
        try {
            return await ReadResponseBodyAsync(content, operation, model, deadlineToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (!callerToken.IsCancellationRequested) {
            RecordAiRequest(operation, model, "timeout");
            return (false, null, Errors.Ai.OpenAiFailed("OpenAI request deadline expired."));
        }
    }

    private static (bool IsSuccess, JsonDocument? Json, Error Error, bool CanFallback) ParseSuccessfulResponse(
        string responseBody,
        string operation,
        string model) {
        try {
            var json = JsonDocument.Parse(responseBody, new JsonDocumentOptions {
                MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
            });
            RecordAiRequest(operation, model, "success");
            return (true, json, Error.None, false);
        } catch (JsonException) {
            RecordAiRequest(operation, model, "invalid_json");
            return (false, null, Errors.Ai.InvalidResponse("OpenAI returned an invalid JSON response."), true);
        }
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

        if (response.StatusCode == HttpStatusCode.TooManyRequests) {
            if (attempt < MaxTransientRetries) {
                logger.LogWarning(
                    "OpenAI transient failure on attempt {Attempt}. Status={Status} RequestId={RequestId} Summary={Summary}. Retrying.",
                    attempt + 1,
                    statusCode,
                    requestId ?? "n/a",
                    summary);
                await Task.Delay(ResolveRetryDelay(response, attempt), _timeProvider, cancellationToken).ConfigureAwait(false);
            } else {
                logger.LogWarning(
                    "OpenAI transient failure exhausted retries. Status={Status} RequestId={RequestId} Summary={Summary}",
                    statusCode,
                    requestId ?? "n/a",
                    summary);
            }

            if (attempt < MaxTransientRetries) {
                return (true, Error.None);
            }

            RecordAiRequest(operation, model, "rate_limit_exhausted");
            return (false, Errors.Ai.OpenAiFailed("OpenAI rate limit retries were exhausted."));
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
        string promptTemplate,
        int maxOutputTokens) {
        string language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        bool includeLocal = !string.Equals(language, "en", StringComparison.Ordinal);
        string languageHint = includeLocal
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";
        string descriptionHint = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"User hint: {description.Trim()}. ";
        const string locationHint =
            "For every visible food or drink, estimate its visual center in the image. " +
            "Return centerX and centerY normalized from 0 at the left/top edge to 1 at the right/bottom edge, " +
            "plus locationConfidence from 0 to 1. Use null for all three fields only when the item cannot be localized.";

        string resolvedPrompt = promptTemplate
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal)
            .Replace("{{descriptionHint}}", descriptionHint, StringComparison.Ordinal);

        return new {
            model,
            max_output_tokens = maxOutputTokens,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = descriptionHint + resolvedPrompt + " " + locationHint,
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

    private static object BuildTextParseRequest(
        string model,
        string text,
        string? userLanguage,
        string promptTemplate,
        int maxOutputTokens) {
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
            max_output_tokens = maxOutputTokens,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt + " Set centerX, centerY, and locationConfidence to null because no image was provided.",
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
                                    centerX = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                    centerY = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                    locationConfidence = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                },
                                required = new[] {
                                    "nameEn",
                                    "nameLocal",
                                    "amount",
                                    "unit",
                                    "confidence",
                                    "centerX",
                                    "centerY",
                                    "locationConfidence",
                                },
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

    private static object BuildNutritionRequest(
        string model,
        IReadOnlyList<FoodVisionItemModel> items,
        string promptTemplate,
        int maxOutputTokens) {
        var mappedItems = items.Select(item => new {
            name = string.IsNullOrWhiteSpace(item.NameEn) ? item.NameLocal ?? "unknown" : item.NameEn,
            amount = item.Amount,
            unit = item.Unit,
        });

        string itemsJson = JsonSerializer.Serialize(new { items = mappedItems });
        string resolvedPrompt = promptTemplate
            .Replace("{{itemsJson}}", itemsJson, StringComparison.Ordinal);

        return new {
            model,
            max_output_tokens = maxOutputTokens,
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
        => new() {
            PropertyNameCaseInsensitive = true,
            MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
        };

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

    private TimeSpan ResolveRetryDelay(HttpResponseMessage response, int attempt) {
        TimeSpan delay = response.Headers.RetryAfter?.Delta ?? RetryDelays[attempt];
        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryAt) {
            delay = retryAt - _timeProvider.GetUtcNow();
        }

        if (delay <= TimeSpan.Zero) {
            return TimeSpan.Zero;
        }

        return delay > MaximumRetryDelay ? MaximumRetryDelay : delay;
    }

    private static string SummarizeErrorBody(string responseBody) {
        if (string.IsNullOrWhiteSpace(responseBody)) {
            return "empty";
        }

        try {
            var root = JsonNode.Parse(
                responseBody,
                documentOptions: new JsonDocumentOptions {
                    MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
                });
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
