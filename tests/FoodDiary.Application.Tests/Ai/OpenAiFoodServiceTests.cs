using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            CreateAiUserContextService(user),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            user.Id,
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            CreateAiUserContextService(returnNull: true),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            userId,
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(52m, result.Value.Calories);
        Assert.Equal(1, client.CalculateNutritionCalls);
        Assert.Single(usageRepository.Items);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            CalculateNutritionResult = Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(Errors.Ai.EmptyItems()),
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
        Assert.Empty(usageRepository.Items);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenClientSucceeds_StoresUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var service = new OpenAiFoodService(
            new RecordingOpenAiFoodClient(),
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            CancellationToken.None);

        ResultAssert.Success(result);
        AiUsage usage = Assert.Single(usageRepository.Items);
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
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            CreateAiUserContextService(returnNull: true),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            userId,
            description: null,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(0, client.AnalyzeFoodImageCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenOutputQuotaExceeded_ReturnsQuotaErrorWithoutCallingClient() {
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            new RecordingAiUsageRepository(new AiUsageTotals(0, 1_000_000)),
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.AnalyzeFoodImageCalls);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            AnalyzeFoodImageResult = Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.Forbidden()),
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            CancellationToken.None);

        ResultAssert.Failure(result);
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
                Usage: null)),
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(usageRepository.Items);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenClientSucceeds_ReturnsVisionAndStoresUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient();
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        ResultAssert.Success(result);
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
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(0, client.ParseFoodTextCalls);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenClientFails_ReturnsFailureWithoutUsage() {
        var usageRepository = new RecordingAiUsageRepository(new AiUsageTotals(0, 0));
        var client = new RecordingOpenAiFoodClient {
            ParseFoodTextResult = Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.EmptyItems()),
        };
        var service = new OpenAiFoodService(
            client,
            usageRepository,
            CreateAiUserContextService(),
            new StubDateTimeProvider(),
            CreateAiPromptProvider());

        Result<FoodVisionModel> result = await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
        Assert.Empty(usageRepository.Items);
    }

    private static FoodVisionModel CreateVisionModel() =>
        new([
            new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m),
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

    private static IAiUserContextService CreateAiUserContextService(User? user = null, bool returnNull = false) {
        User resolvedUser = user ?? User.Create("ai-tests@example.com", "hash");
        IAiUserContextService service = Substitute.For<IAiUserContextService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                if (returnNull) {
                    return Task.FromResult(Result.Failure<AiUserContext>(Errors.User.NotFound()));
                }

                if (!resolvedUser.IsActive || resolvedUser.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<AiUserContext>(Errors.Authentication.InvalidToken));
                }

                return Task.FromResult(Result.Success(new AiUserContext(
                    resolvedUser.Id,
                    resolvedUser.Language,
                    resolvedUser.AiInputTokenLimit,
                    resolvedUser.AiOutputTokenLimit)));
            });

        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    private static IAiPromptProvider CreateAiPromptProvider() {
        IAiPromptProvider provider = Substitute.For<IAiPromptProvider>();
        provider
            .GetPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<string>(0)));

        return provider;
    }
}
