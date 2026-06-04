using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
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
    public async Task CalculateNutritionAsync_WhenUserInactive_ReturnsAccessErrorWithoutCallingClient() {
        var user = User.Create("inactive-ai-service@example.com", "hash");
        user.Deactivate();
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(0, 0)),
            new StubUserRepository(user),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            user.Id,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, client.CalculateNutritionCalls);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenUserMissing_ReturnsNotFoundWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var userId = UserId.New();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(0, 0)),
            new StubUserRepository(returnNull: true),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            userId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(0, client.CalculateNutritionCalls);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientSucceeds_ReturnsNutritionAndStoresUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(52m, result.Value.Calories);
        Assert.Equal(1, client.CalculateNutritionCalls);
        Assert.Single(usageRepository.Items);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            CalculateNutritionResult = Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(Errors.Ai.EmptyItems())
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
        Assert.Empty(usageRepository.Items);
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

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(5_000_000, 0)),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.AnalyzeFoodImageCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenUserMissing_ReturnsNotFoundWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var userId = UserId.New();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(0, 0)),
            new StubUserRepository(returnNull: true),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            userId,
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(0, client.AnalyzeFoodImageCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenOutputQuotaExceeded_ReturnsQuotaErrorWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(0, 1_000_000)),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.AnalyzeFoodImageCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            AnalyzeFoodImageResult = Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.Forbidden())
        };
        var service = new OpenAiFoodService(
            client,
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

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.Forbidden", result.Error.Code);
        Assert.Empty(usageRepository.Items);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenResponseHasNoUsage_DoesNotStoreUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            AnalyzeFoodImageResult = Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "vision",
                "test-model",
                Usage: null))
        };
        var service = new OpenAiFoodService(
            client,
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
        Assert.Empty(usageRepository.Items);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenClientSucceeds_ReturnsVisionAndStoresUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, client.ParseFoodTextCalls);
        Assert.Single(usageRepository.Items);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(5_000_000, 0)),
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.ParseFoodTextCalls);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            ParseFoodTextResult = Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.EmptyItems())
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            new StubUserRepository(),
            new StubDateTimeProvider(),
            new StubAiPromptProvider());

        var result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
        Assert.Empty(usageRepository.Items);
    }

    private static FoodVisionModel CreateVisionModel() =>
        new([
            new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)
        ]);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingOpenAiFoodClient : IOpenAiFoodClient {
        public Result<OpenAiFoodClientResponse<FoodVisionModel>>? AnalyzeFoodImageResult { get; init; }
        public Result<OpenAiFoodClientResponse<FoodVisionModel>>? ParseFoodTextResult { get; init; }
        public Result<OpenAiFoodClientResponse<FoodNutritionModel>>? CalculateNutritionResult { get; init; }

        public int AnalyzeFoodImageCalls { get; private set; }
        public int ParseFoodTextCalls { get; private set; }
        public int CalculateNutritionCalls { get; private set; }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> AnalyzeFoodImageAsync(
            string imageUrl,
            string? userLanguage,
            string? description,
            string promptTemplate,
            CancellationToken cancellationToken) {
            AnalyzeFoodImageCalls++;
            if (AnalyzeFoodImageResult is not null) {
                return Task.FromResult(AnalyzeFoodImageResult);
            }

            return Task.FromResult(Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "vision",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> ParseFoodTextAsync(
            string text,
            string? userLanguage,
            string promptTemplate,
            CancellationToken cancellationToken) {
            ParseFoodTextCalls++;
            if (ParseFoodTextResult is not null) {
                return Task.FromResult(ParseFoodTextResult);
            }

            return Task.FromResult(Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "text-parse",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }

        public Task<Result<OpenAiFoodClientResponse<FoodNutritionModel>>> CalculateNutritionAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            string promptTemplate,
            CancellationToken cancellationToken) {
            CalculateNutritionCalls++;
            if (CalculateNutritionResult is not null) {
                return Task.FromResult(CalculateNutritionResult);
            }

            return Task.FromResult(Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(
                new FoodNutritionModel(52m, 0.3m, 0.2m, 14m, 2.4m, 0m, []),
                "nutrition",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user = null, bool returnNull = false) : IUserRepository {
        private readonly User user = user ?? User.Create("ai-tests@example.com", "hash");

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(returnNull ? null : user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubAiPromptProvider : IAiPromptProvider {
        public Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(key);
    }
}
