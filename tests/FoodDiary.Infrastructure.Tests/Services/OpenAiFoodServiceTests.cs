using System.Diagnostics.Metrics;
using System.Net;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class OpenAiFoodServiceTests {
    private const string IntegrationsMeterName = "FoodDiary.Integrations";
    private const string VisionPrompt = "Analyze image. {{languageHint}} {{descriptionHint}}";
    private const string NutritionPrompt = "Estimate nutrition for {{itemsJson}}.";

    [Fact]
    public async Task CalculateNutritionAsync_WhenTransportFails_ReturnsOpenAiFailedError() {
        long? requestCount = null;
        string? outcome = null;
        using var listener = CreateIntegrationsListener(
            onRequest: (value, tags) => {
                requestCount = value;
                outcome = GetTagValue(tags, "fooddiary.ai.outcome");
            },
            onFallback: null);

        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")));
        var client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        var result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
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
                Content = new StringContent("{\"error\":\"temporary\"}")
            },
            CreateVisionSuccessResponse()
        ]);

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        var client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback"
        });

        var result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Value.Items);
        Assert.Equal("vision", result.Value.Operation);
        Assert.Equal("vision-primary", result.Value.Model);
        Assert.Equal(18, result.Value.Usage?.TotalTokens);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenPrimaryFails_UsesFallbackAndRecordsMetric() {
        long? fallbackCount = null;
        using var listener = CreateIntegrationsListener(
            onRequest: null,
            onFallback: (value, _) => fallbackCount = value);

        var responses = new Queue<HttpResponseMessage>([
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"bad request\"}")
            },
            CreateVisionSuccessResponse()
        ]);

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        var client = CreateClient(httpClient, new OpenAiOptions {
            ApiKey = "test-key",
            VisionModel = "vision-primary",
            VisionFallbackModel = "vision-fallback"
        });

        var result = await client.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            null,
            VisionPrompt,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("vision-fallback", result.Value.Model);
        Assert.Equal(1, fallbackCount);
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
                    """)
            }
        ])));
        var client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        var result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            NutritionPrompt,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
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
                    """)
            }
        ])));
        var client = CreateClient(httpClient, new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" });

        var result = await client.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
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
                """)
        };
    }

    private static MeterListener CreateIntegrationsListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onRequest,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onFallback) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != IntegrationsMeterName) {
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
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class SequenceHttpMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (responses.Count == 0) {
                throw new InvalidOperationException("No more responses configured.");
            }

            return Task.FromResult(responses.Dequeue());
        }
    }
}
