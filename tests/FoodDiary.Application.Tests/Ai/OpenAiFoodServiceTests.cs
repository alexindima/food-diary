using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Ai;

public sealed class OpenAiFoodServiceTests {
    [Fact]
    public async Task CalculateNutritionAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(5_000_000, 0)),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.CalculateNutritionCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenClientSucceeds_StoresUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var service = new OpenAiFoodService(
            new RecordingOpenAiFoodClient(),
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
        Assert.Equal("vision", usage.Operation);
        Assert.Equal("test-model", usage.Model);
        Assert.Equal(18, usage.TotalTokens);
    }

    private sealed class RecordingOpenAiFoodClient : IOpenAiFoodClient {
        public int CalculateNutritionCalls { get; private set; }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> AnalyzeFoodImageAsync(
            string imageUrl,
            string? userLanguage,
            string? description,
            string promptTemplate,
            CancellationToken cancellationToken) {
            var model = new FoodVisionModel([
                new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)
            ]);

            return Task.FromResult(Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                model,
                "vision",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> ParseFoodTextAsync(
            string text,
            string? userLanguage,
            string promptTemplate,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<OpenAiFoodClientResponse<FoodNutritionModel>>> CalculateNutritionAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            string promptTemplate,
            CancellationToken cancellationToken) {
            CalculateNutritionCalls++;
            return Task.FromResult(Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(
                new FoodNutritionModel(52m, 0.3m, 0.2m, 14m, 2.4m, 0m, []),
                "nutrition",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }
    }

    private sealed class RecordingAiUsageRepository(AiUsageTotals totals) : IAiUsageRepository {
        public List<AiUsage> Items { get; } = [];

        public Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) {
            Items.Add(usage);
            return Task.CompletedTask;
        }

        public Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageSummary(0, 0, 0, [], [], [], []));

        public Task<AiUsageTotals> GetUserTotalsAsync(UserId userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(totals);
    }

    private sealed class StubUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(User.Create("ai-tests@example.com", "hash"));
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
        public Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(key);
    }
}
