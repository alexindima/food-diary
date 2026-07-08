using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class OpenAiFoodServiceTests {
    private const string IntegrationsMeterName = "FoodDiary.Integrations";
    private const string VisionPrompt = "Analyze image. {{languageHint}} {{descriptionHint}}";
    private const string TextPrompt = "Parse text '{{userText}}'. {{languageHint}}";
    private const string NutritionPrompt = "Estimate nutrition for {{itemsJson}}.";

    [Theory]
    [InlineData("vision")]
    [InlineData("text")]
    [InlineData("nutrition")]
    public async Task OpenAiOperations_WhenApiKeyIsMissing_ReturnOpenAiFailedError(string operation) {
        using var httpClient = new HttpClient(new CountingHttpMessageHandler(_ => CreateVisionSuccessResponse()));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions());

        Result result = operation switch {
            "vision" => await client.AnalyzeFoodImageAsync(
                "https://cdn.example.com/meal.webp",
                "en",
                description: null,
                VisionPrompt,
                CancellationToken.None),
            "text" => await client.ParseFoodTextAsync("apple 100g", "en", TextPrompt, CancellationToken.None),
            _ => await client.CalculateNutritionAsync(
                [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
                NutritionPrompt,
                CancellationToken.None),
        };

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("API key", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenTransportFails_ReturnsOpenAiFailedError() {
        long? requestCount = null;
        string? outcome = null;
        using MeterListener listener = CreateIntegrationsListener(
            onRequest: (value, tags) => {
                requestCount = value;
                outcome = GetTagValue(tags, "fooddiary.ai.outcome");
            },
            onFallback: null);

        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("transport error", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, requestCount);
        Assert.Equal("transport_error", outcome);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenTransientFailureOccurs_RetriesPrimaryModelAndReturnsUsage() {
        var responses = new Queue<HttpResponseMessage>([
            new(HttpStatusCode.InternalServerError) {
                Content = new StringContent("{\"error\":\"temporary\"}"),
            },
            CreateVisionSuccessResponse(),
        ]);

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            description: null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Value.Items);
        Assert.Equal("vision", result.Value.Operation);
        Assert.Equal("vision-primary", result.Value.Model);
        Assert.Equal(18, result.Value.Usage?.TotalTokens);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenTransientFailuresExhaustRetries_ReturnsRetryExhaustedError() {
        long? requestCount = null;
        string? outcome = null;
        using MeterListener listener = CreateIntegrationsListener(
            onRequest: (value, tags) => {
                requestCount = value;
                outcome = GetTagValue(tags, "fooddiary.ai.outcome");
            },
            onFallback: null);
        var handler = new DelegateHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) {
            Content = new StringContent("{\"error\":\"temporarily unavailable\"}"),
        });
        using var httpClient = new HttpClient(handler);
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("after retries", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, handler.CallCount);
        Assert.Equal(1, requestCount);
        Assert.Equal("retry_exhausted", outcome);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenPrimaryFails_UsesFallbackAndRecordsMetric() {
        long? fallbackCount = null;
        using MeterListener listener = CreateIntegrationsListener(
            onRequest: null,
            onFallback: (value, _) => fallbackCount = value);

        var responses = new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"bad request\"}"),
            },
            CreateVisionSuccessResponse(),
        ]);

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            description: null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("vision-fallback", result.Value.Model);
        Assert.Equal(1, fallbackCount);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenPrimaryAndFallbackFail_ReturnsFallbackError() {
        var responses = new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"primary rejected\"}"),
            },
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"fallback rejected\"}"),
            },
        ]);

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            description: null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("fallback rejected", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenParsedVisionFails_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            CreateOpenAiSuccessResponse("not-json"),
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            description: null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Vision JSON invalid", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenOutputTextIsMissing_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.OK) {
                Content = new StringContent("""{"output":[{"content":[]}]}"""),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            description: null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Missing output text", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenRequestSucceeds_ReturnsVisionAndUsesLocalLanguageHint() {
        using var httpClient = new HttpClient(new CapturingHttpMessageHandler(_ => CreateVisionSuccessResponse()));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            TextModel = "text-model",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.ParseFoodTextAsync(
            "apple 100g",
            " RU ",
            TextPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("text-parse", result.Value.Operation);
        Assert.Equal("text-model", result.Value.Model);
        Assert.Contains("apple 100g", CapturingHttpMessageHandler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("nameLocal in language", CapturingHttpMessageHandler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("ru", CapturingHttpMessageHandler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenLanguageIsBlank_UsesEnglishLanguageHint() {
        using var httpClient = new HttpClient(new CapturingHttpMessageHandler(_ => CreateVisionSuccessResponse()));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            TextModel = "text-model",
        });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.ParseFoodTextAsync(
            "apple 100g",
            " ",
            TextPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("set nameLocal to null", CapturingHttpMessageHandler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenOpenAiReturnsFailure_ReturnsError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"bad text\"}"),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "text-model" });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.ParseFoodTextAsync(
            "apple 100g",
            "en",
            TextPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("bad text", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenParsedVisionFails_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            CreateOpenAiSuccessResponse("not-json"),
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "text-model" });

        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await client.ParseFoodTextAsync(
            "apple 100g",
            "en",
            TextPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Vision JSON invalid", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenResponseJsonIsInvalid_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.OK) {
                Content = new StringContent("""
                    {
                      "output": [
                        {
                          "content": [
                            {
                              "type": "output_text",
                              "text": "{not-json}"
                            }
                          ]
                        }
                      ]
                    }
                    """),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenOpenAiReturnsSuccess_ReturnsNutritionAndUsage() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            CreateNutritionSuccessResponse(),
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("", "Apple local", 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("nutrition", result.Value.Operation);
        Assert.Equal("test-model", result.Value.Model);
        Assert.Equal(52m, result.Value.Value.Calories);
        Assert.Equal(18, result.Value.Usage?.TotalTokens);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenOutputTextIsMissing_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.OK) {
                Content = new StringContent("""{"output":[{"content":[]}]}"""),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Missing output text", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenParsedNutritionIsNull_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            CreateOpenAiSuccessResponse("null"),
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Nutrition response is empty", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenResponseBodyIsMalformedJson_ReturnsInvalidJsonError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.OK) {
                Content = new StringContent("not-json"),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
        Assert.Contains("Invalid JSON response", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenFirstRequestTimesOut_RetriesAndReturnsSuccess() {
        var handler = new DelegateHttpMessageHandler((callCount, _) => {
            if (callCount == 1) {
                throw new TaskCanceledException("timeout");
            }

            return CreateNutritionSuccessResponse();
        });
        using var httpClient = new HttpClient(handler);
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenRequestsTimeOut_ReturnsTimeoutError() {
        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new TaskCanceledException("timeout")));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("timed out", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("""{"error":{"type":"invalid_request_error","code":"bad_code","message":"Bad request."}}""", "bad_code")]
    [InlineData("""{"error":{}}""", "response body unavailable")]
    [InlineData("not-json", "response body unavailable")]
    [InlineData("null", "response body unavailable")]
    public async Task CalculateNutritionAsync_WhenErrorBodyVaries_ReturnsSanitizedSummary(string responseBody, string expectedSummary) {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent(responseBody),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedSummary, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractOutputText_WhenOutputIsMissing_ReturnsNull() {
        using var json = JsonDocument.Parse("""{"usage":{"total_tokens":1}}""");

        Assert.Null(InvokePrivateStatic<string?>("ExtractOutputText", json));
    }

    [Fact]
    public void ExtractOutputText_WhenContentIsMissing_ContinuesAndReturnsNextText() {
        using var json = JsonDocument.Parse("""
            {
              "output": [
                {},
                {
                  "content": [
                    {
                      "type": "output_text",
                      "text": "done"
                    }
                  ]
                }
              ]
            }
            """);

        Assert.Equal("done", InvokePrivateStatic<string?>("ExtractOutputText", json));
    }

    [Fact]
    public void ExtractOutputText_WhenContentHasNoOutputText_ReturnsNull() {
        using var json = JsonDocument.Parse("""
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "input_text",
                      "text": "ignored"
                    }
                  ]
                }
              ]
            }
            """);

        Assert.Null(InvokePrivateStatic<string?>("ExtractOutputText", json));
    }

    [Fact]
    public void ExtractUsage_WhenUsageIsMissing_ReturnsNull() {
        using var json = JsonDocument.Parse("""{"output":[]}""");

        Assert.Null(InvokePrivateStatic<AiUsageTokens?>("ExtractUsage", json));
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenErrorResponseContainsPromptData_DoesNotExposeRawBodyInErrorMessage() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("""
                    {
                      "error": {
                        "type": "invalid_request_error",
                        "message": "Request rejected."
                      },
                      "debugPrompt": "user uploaded salmon salad with private note"
                    }
                    """),
            },
        ])));
        OpenAiFoodClient client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("Request rejected.", result.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("salmon salad", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("debugPrompt", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OpenAiFoodClient CreateClient(HttpClient httpClient, OpenAiOptions options) {
        return new OpenAiFoodClient(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<OpenAiFoodClient>.Instance);
    }

    private static HttpResponseMessage CreateVisionSuccessResponse() {
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""
                {
                  "output": [
                    {
                      "content": [
                        {
                          "type": "output_text",
                          "text": "{\"items\":[{\"nameEn\":\"Apple\",\"nameLocal\":null,\"amount\":100,\"unit\":\"g\",\"confidence\":0.97}]}"
                        }
                      ]
                    }
                  ],
                  "usage": {
                    "input_tokens": 11,
                    "output_tokens": 7,
                    "total_tokens": 18
                  }
                }
                """),
        };
    }

    private static HttpResponseMessage CreateNutritionSuccessResponse() {
        return CreateOpenAiSuccessResponse("""
            {
              "calories": 52,
              "protein": 0.3,
              "fat": 0.2,
              "carbs": 14,
              "fiber": 2.4,
              "alcohol": 0,
              "items": [
                {
                  "name": "Apple",
                  "amount": 100,
                  "unit": "g",
                  "calories": 52,
                  "protein": 0.3,
                  "fat": 0.2,
                  "carbs": 14,
                  "fiber": 2.4,
                  "alcohol": 0
                }
              ]
            }
            """);
    }

    private static HttpResponseMessage CreateOpenAiSuccessResponse(string outputText) {
        string escapedOutputText = JsonSerializer.Serialize(outputText);
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent($$"""
                {
                  "output": [
                    {
                      "content": [
                        {
                          "type": "output_text",
                          "text": {{escapedOutputText}}
                        }
                      ]
                    }
                  ],
                  "usage": {
                    "input_tokens": 11,
                    "output_tokens": 7,
                    "total_tokens": 18
                  }
                }
                """),
        };
    }

    private static T? InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(OpenAiFoodClient).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T?)method.Invoke(null, args);
    }

    private static MeterListener CreateIntegrationsListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onRequest,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onFallback) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, IntegrationsMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name is "fooddiary.ai.requests" or "fooddiary.ai.fallbacks") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            switch (instrument.Name) {
                case "fooddiary.ai.requests":
                    onRequest?.Invoke(value, tags);
                    break;
                case "fooddiary.ai.fallbacks":
                    onFallback?.Invoke(value, tags);
                    break;
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SequenceHttpMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (responses.Count == 0) {
                throw new InvalidOperationException("No more responses configured.");
            }

            return Task.FromResult(responses.Dequeue());
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public static string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return responseFactory(request);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class DelegateHttpMessageHandler(Func<int, HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            CallCount++;
            return Task.FromResult(responseFactory(CallCount, request));
        }
    }
}
