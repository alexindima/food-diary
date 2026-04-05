using System.Net;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class OpenAiFoodServiceTests {
    private const string InfrastructureMeterName = "FoodDiary.Infrastructure";

    [Fact]
    public async Task CalculateNutritionAsync_WhenTransportFails_ReturnsOpenAiFailedError() {
        long? requestCount = null;
        string? outcome = null;
        using var listener = CreateInfrastructureListener(
            onRequest: (value, tags) => {
                requestCount = value;
                outcome = GetTagValue(tags, "fooddiary.ai.outcome");
            },
            onQuotaRejection: null,
            onFallback: null);

        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")));
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("transport error", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, requestCount);
        Assert.Equal("transport_error", outcome);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenTransientFailureOccurs_RetriesPrimaryModelAndStoresUsage() {
        var responses = new Queue<HttpResponseMessage>(new HttpResponseMessage[] {
            new(HttpStatusCode.InternalServerError) {
                Content = new StringContent("{\"error\":\"temporary\"}")
            },
            new(HttpStatusCode.OK) {
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
            }
        });

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        var usageRepository = new RecordingAiUsageRepository();
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions {
                ApiKey = "test-key",
                VisionModel = "vision-primary",
                VisionFallbackModel = "vision-fallback"
            }),
            NullLogger<OpenAiFoodService>.Instance,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        var usage = Assert.Single(usageRepository.Items);
        Assert.Equal("vision", usage.Operation);
        Assert.Equal("vision-primary", usage.Model);
        Assert.Equal(18, usage.TotalTokens);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenPrimaryFails_UsesFallbackAndRecordsMetric() {
        long? fallbackCount = null;
        using var listener = CreateInfrastructureListener(
            onRequest: null,
            onQuotaRejection: null,
            onFallback: (value, _) => fallbackCount = value);

        var responses = new Queue<HttpResponseMessage>(new HttpResponseMessage[] {
            new(HttpStatusCode.BadRequest) {
                Content = new StringContent("{\"error\":\"bad request\"}")
            },
            new(HttpStatusCode.OK) {
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
            }
        });

        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(responses));
        var usageRepository = new RecordingAiUsageRepository();
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions {
                ApiKey = "test-key",
                VisionModel = "vision-primary",
                VisionFallbackModel = "vision-fallback"
            }),
            NullLogger<OpenAiFoodService>.Instance,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var usage = Assert.Single(usageRepository.Items);
        Assert.Equal("vision-fallback", usage.Model);
        Assert.Equal(1, fallbackCount);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingOpenAi() {
        long? quotaRejectionCount = null;
        using var listener = CreateInfrastructureListener(
            onRequest: null,
            onQuotaRejection: (value, _) => quotaRejectionCount = value,
            onFallback: null);

        var handler = new CountingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(new AiUsageTotals(5_000_000, 0)),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, handler.CallCount);
        Assert.Equal(1, quotaRejectionCount);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenResponseJsonIsInvalid_ReturnsInvalidResponseError() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>(new HttpResponseMessage[] {
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
        })));
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenErrorResponseContainsPromptData_DoesNotExposeRawBodyInErrorMessage() {
        using var httpClient = new HttpClient(new SequenceHttpMessageHandler(new Queue<HttpResponseMessage>(new HttpResponseMessage[] {
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
        })));
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("Request rejected.", result.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("salmon salad", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("debugPrompt", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onRequest,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onQuotaRejection,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onFallback) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != InfrastructureMeterName) {
                return;
            }

            if (instrument.Name is "fooddiary.ai.requests" or "fooddiary.ai.quota_rejections" or "fooddiary.ai.fallbacks") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            switch (instrument.Name) {
                case "fooddiary.ai.requests":
                    onRequest?.Invoke(value, tags);
                    break;
                case "fooddiary.ai.quota_rejections":
                    onQuotaRejection?.Invoke(value, tags);
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

    private sealed class SequenceHttpMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (responses.Count == 0) {
                throw new InvalidOperationException("No more responses configured.");
            }

            return Task.FromResult(responses.Dequeue());
        }
    }

    private sealed class CountingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class StubAiUsageRepository(AiUsageTotals? totals = null) : IAiUsageRepository {
        public Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageSummary(0, 0, 0, [], [], [], []));

        public Task<AiUsageTotals> GetUserTotalsAsync(UserId userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(totals ?? new AiUsageTotals(0, 0));
    }

    private sealed class RecordingAiUsageRepository : IAiUsageRepository {
        public List<AiUsage> Items { get; } = [];

        public Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) {
            Items.Add(usage);
            return Task.CompletedTask;
        }

        public Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageSummary(0, 0, 0, [], [], [], []));

        public Task<AiUsageTotals> GetUserTotalsAsync(UserId userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageTotals(0, 0));
    }

    private sealed class StubUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) {
            var user = User.Create("ai-tests@example.com", "hash");
            return Task.FromResult<User?>(user);
        }
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class StubAiPromptProvider : IAiPromptProvider {
        private static readonly Dictionary<string, string> Prompts = new(StringComparer.OrdinalIgnoreCase) {
            ["vision"] = "Analyze the food photo and return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. {{languageHint}}",
            ["text-parse"] = "Parse the following food description into structured items: \"{{userText}}\". Return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. Estimate typical portion sizes for items without explicit amounts. {{languageHint}}",
            ["nutrition"] = "You are a nutrition assistant. Using the provided items with amounts, estimate calories and nutrients per item and totals. Item names are in English. Return only JSON.",
        };

        public Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Prompts.TryGetValue(key, out var prompt) ? prompt : key);
    }
}
