using System.Net;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class OpenAiFoodServiceTests {
    [Fact]
    public async Task CalculateNutritionAsync_WhenTransportFails_ReturnsOpenAiFailedError() {
        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")));
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(),
            new StubUserRepository());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("transport error", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
            new StubUserRepository());

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
    public async Task CalculateNutritionAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingOpenAi() {
        var handler = new CountingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(new AiUsageTotals(5_000_000, 0)),
            new StubUserRepository());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, handler.CallCount);
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
            new StubUserRepository());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.InvalidResponse", result.Error.Code);
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
}
