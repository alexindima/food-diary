using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class OpenAiFoodServiceTests {
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task CalculateNutritionAsync_WhenQuotaExceeded_ReturnsQuotaErrorWithoutCallingProvider() {
        var client = new RecordingOpenAiFoodClient();
        var quotaRepository = new RecordingAiQuotaRepository {
            ReserveStatus = AiQuotaReservationStatus.QuotaExceeded,
        };
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        Assert.Equal(1, client.CalculateNutritionBudgetCalls);
        Assert.Equal(0, client.CalculateNutritionCalls);
        Assert.Single(quotaRepository.Reservations);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenUserInactive_DoesNotCountOrReserveTokens() {
        var user = User.Create("inactive-ai-service@example.com", "hash");
        user.Deactivate();
        var client = new RecordingOpenAiFoodClient();
        var quotaRepository = new RecordingAiQuotaRepository();
        OpenAiFoodService service = CreateService(client, quotaRepository, user);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            user.Id,
            RequestId,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, client.CalculateNutritionBudgetCalls);
        Assert.Empty(quotaRepository.Reservations);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenTokenCountFails_DoesNotReserveOrGenerate() {
        var client = new RecordingOpenAiFoodClient {
            CalculateNutritionBudgetResult = Result.Failure<AiProviderTokenBudget>(Errors.Ai.InvalidResponse("count failed")),
        };
        var quotaRepository = new RecordingAiQuotaRepository();
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(0, client.CalculateNutritionCalls);
        Assert.Empty(quotaRepository.Reservations);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientSucceeds_ReservesBeforeGenerationAndReconcilesActualUsage() {
        var quotaRepository = new RecordingAiQuotaRepository();
        var client = new RecordingOpenAiFoodClient {
            BeforeCalculateNutrition = () => Assert.Single(quotaRepository.Reservations),
        };
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(52m, result.Value.Calories);
        AiQuotaReservationRequest reservation = Assert.Single(quotaRepository.Reservations);
        Assert.Multiple(
            () => Assert.Equal(RequestId, reservation.RequestId),
            () => Assert.Equal("nutrition", reservation.Operation),
            () => Assert.Equal(11, reservation.InputTokens),
            () => Assert.Equal(4_096, reservation.OutputTokens));
        AiQuotaUsage reconciliation = Assert.Single(quotaRepository.Reconciliations);
        Assert.Equal(new AiQuotaUsage("nutrition", "test-model", 11, 7, 18), reconciliation);
        Assert.Empty(quotaRepository.Releases);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientFailureCostIsUnknown_KeepsReservationPending() {
        var quotaRepository = new RecordingAiQuotaRepository();
        var client = new RecordingOpenAiFoodClient {
            CalculateNutritionResult = Result.Failure<OpenAiFoodClientResponse<FoodNutritionModel>>(Errors.Ai.EmptyItems()),
        };
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
        Assert.Empty(quotaRepository.Releases);
        Assert.Empty(quotaRepository.Reconciliations);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenClientDisconnectsAfterProviderResponse_ReconcilesWithServerOwnedToken() {
        using var requestCancellation = new CancellationTokenSource();
        var quotaRepository = new RecordingAiQuotaRepository();
        var client = new RecordingOpenAiFoodClient {
            BeforeCalculateNutrition = requestCancellation.Cancel,
        };
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            requestCancellation.Token);

        ResultAssert.Success(result);
        Assert.Single(quotaRepository.Reconciliations);
        Assert.False(quotaRepository.ReconcileTokenWasCanceled);
    }

    [Fact]
    public async Task AnalyzeFoodImageAsync_WhenResponseHasNoUsage_ChargesReservedBudgetConservatively() {
        var quotaRepository = new RecordingAiQuotaRepository();
        var client = new RecordingOpenAiFoodClient {
            AnalyzeFoodImageResult = Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "vision",
                "test-model",
                Usage: null)),
        };
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodVisionModel> result = await service.AnalyzeFoodImageAsync(
            "https://cdn.example.com/meal.webp",
            "en",
            UserId.New(),
            description: null,
            RequestId,
            CancellationToken.None);

        ResultAssert.Success(result);
        AiQuotaUsage usage = Assert.Single(quotaRepository.Reconciliations);
        Assert.Equal(new AiQuotaUsage("vision", "test-model", 11, 4_096, 4_107), usage);
    }

    [Fact]
    public async Task ParseFoodTextAsync_WhenClientSucceeds_UsesStableRequestIdAndReconciles() {
        var quotaRepository = new RecordingAiQuotaRepository();
        var client = new RecordingOpenAiFoodClient();
        OpenAiFoodService service = CreateService(client, quotaRepository);

        Result<FoodVisionModel> result = await service.ParseFoodTextAsync(
            "apple 100g",
            "en",
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(RequestId, Assert.Single(quotaRepository.Reservations).RequestId);
        Assert.Equal("text-parse", Assert.Single(quotaRepository.Reconciliations).Operation);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenOverallDeadlineExpiresDuringTokenCount_ReturnsProviderFailure() {
        var client = new RecordingOpenAiFoodClient {
            BeforeCalculateNutritionBudgetAsync = static cancellationToken =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
        };
        var quotaRepository = new RecordingAiQuotaRepository();
        OpenAiFoodService service = CreateService(
            client,
            quotaRepository,
            overallOperationTimeout: TimeSpan.FromMilliseconds(25));

        Result<FoodNutritionModel> result = await service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Ai.OpenAiFailed", result.Error.Code),
            () => Assert.Contains("deadline", result.Error.Message, StringComparison.OrdinalIgnoreCase),
            () => Assert.Empty(quotaRepository.Reservations),
            () => Assert.Equal(0, client.CalculateNutritionCalls));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VisionOperations_WhenOverallDeadlineExpiresDuringTokenCount_ReturnProviderFailure(bool analyzeImage) {
        var client = new RecordingOpenAiFoodClient {
            BeforeVisionBudgetAsync = static cancellationToken =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
        };
        var quotaRepository = new RecordingAiQuotaRepository();
        OpenAiFoodService service = CreateService(
            client,
            quotaRepository,
            overallOperationTimeout: TimeSpan.FromMilliseconds(25));

        Result<FoodVisionModel> result = analyzeImage
            ? await service.AnalyzeFoodImageAsync(
                "https://cdn.example.com/meal.webp",
                "en",
                UserId.New(),
                description: null,
                RequestId,
                CancellationToken.None)
            : await service.ParseFoodTextAsync(
                "apple 100g",
                "en",
                UserId.New(),
                RequestId,
                CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Ai.OpenAiFailed", result.Error.Code),
            () => Assert.Contains("deadline", result.Error.Message, StringComparison.OrdinalIgnoreCase),
            () => Assert.Empty(quotaRepository.Reservations));
    }

    [Theory]
    [InlineData(true, "context")]
    [InlineData(true, "budget")]
    [InlineData(true, "quota")]
    [InlineData(true, "provider")]
    [InlineData(false, "context")]
    [InlineData(false, "budget")]
    [InlineData(false, "quota")]
    [InlineData(false, "provider")]
    public async Task VisionOperations_WhenDependencyFails_ReturnFailure(bool analyzeImage, string stage) {
        var client = new RecordingOpenAiFoodClient {
            AnalyzeFoodImageBudgetResult = string.Equals(stage, "budget", StringComparison.Ordinal)
                ? Result.Failure<AiProviderTokenBudget>(Errors.Ai.InvalidResponse("budget failed"))
                : null,
            ParseFoodTextBudgetResult = string.Equals(stage, "budget", StringComparison.Ordinal)
                ? Result.Failure<AiProviderTokenBudget>(Errors.Ai.InvalidResponse("budget failed"))
                : null,
            AnalyzeFoodImageResult = string.Equals(stage, "provider", StringComparison.Ordinal)
                ? Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.InvalidResponse("provider failed"))
                : null,
            ParseFoodTextResult = string.Equals(stage, "provider", StringComparison.Ordinal)
                ? Result.Failure<OpenAiFoodClientResponse<FoodVisionModel>>(Errors.Ai.InvalidResponse("provider failed"))
                : null,
        };
        var quotaRepository = new RecordingAiQuotaRepository {
            ReserveStatus = string.Equals(stage, "quota", StringComparison.Ordinal)
                ? AiQuotaReservationStatus.QuotaExceeded
                : AiQuotaReservationStatus.Acquired,
        };
        OpenAiFoodService service = CreateService(
            client,
            quotaRepository,
            returnNull: string.Equals(stage, "context", StringComparison.Ordinal));

        Result<FoodVisionModel> result = analyzeImage
            ? await service.AnalyzeFoodImageAsync(
                imageUrl: "https://cdn.example.com/meal.webp",
                userLanguage: "en",
                userId: UserId.New(),
                description: null,
                requestId: RequestId,
                cancellationToken: CancellationToken.None)
            : await service.ParseFoodTextAsync("apple 100g", "en", UserId.New(), RequestId, CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CalculateNutritionAsync_WhenCallerCancels_PropagatesCancellation() {
        var client = new RecordingOpenAiFoodClient {
            BeforeCalculateNutritionBudgetAsync = static cancellationToken =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
        };
        var quotaRepository = new RecordingAiQuotaRepository();
        OpenAiFoodService service = CreateService(client, quotaRepository);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CalculateNutritionAsync(
            CreateItems(),
            UserId.New(),
            RequestId,
            cancellation.Token));
    }

    private static OpenAiFoodService CreateService(
        RecordingOpenAiFoodClient client,
        RecordingAiQuotaRepository quotaRepository,
        User? user = null,
        bool returnNull = false,
        TimeSpan? overallOperationTimeout = null) =>
        new(
            client,
            quotaRepository,
            CreateAiUserContextService(user, returnNull),
            new StubDateTimeProvider(),
            CreateAiPromptProvider(),
            overallOperationTimeout);

    private static IReadOnlyList<FoodVisionItemModel> CreateItems() =>
        [new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)];

    private static FoodVisionModel CreateVisionModel() =>
        new([new FoodVisionItemModel("Apple", NameLocal: null, 100m, "g", 0.9m)]);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingOpenAiFoodClient : IOpenAiFoodClient {
        private static readonly AiProviderTokenBudget DefaultBudget = new(11, 4_096);

        public Result<AiProviderTokenBudget>? CalculateNutritionBudgetResult { get; init; }
        public Result<AiProviderTokenBudget>? AnalyzeFoodImageBudgetResult { get; init; }
        public Result<AiProviderTokenBudget>? ParseFoodTextBudgetResult { get; init; }
        public Result<OpenAiFoodClientResponse<FoodVisionModel>>? AnalyzeFoodImageResult { get; init; }
        public Result<OpenAiFoodClientResponse<FoodVisionModel>>? ParseFoodTextResult { get; init; }
        public Result<OpenAiFoodClientResponse<FoodNutritionModel>>? CalculateNutritionResult { get; init; }
        public Action? BeforeCalculateNutrition { get; init; }
        public Func<CancellationToken, Task>? BeforeCalculateNutritionBudgetAsync { get; init; }
        public Func<CancellationToken, Task>? BeforeVisionBudgetAsync { get; init; }

        public int CalculateNutritionBudgetCalls { get; private set; }
        public int CalculateNutritionCalls { get; private set; }

        public async Task<Result<AiProviderTokenBudget>> GetAnalyzeFoodImageTokenBudgetAsync(
            string imageUrl,
            string? userLanguage,
            string? description,
            string promptTemplate,
            CancellationToken cancellationToken) {
            if (BeforeVisionBudgetAsync is not null) {
                await BeforeVisionBudgetAsync(cancellationToken);
            }

            return AnalyzeFoodImageBudgetResult ?? Result.Success(DefaultBudget);
        }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> AnalyzeFoodImageAsync(
            string imageUrl,
            string? userLanguage,
            string? description,
            string promptTemplate,
            CancellationToken cancellationToken) =>
            Task.FromResult(AnalyzeFoodImageResult ?? Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "vision",
                "test-model",
                new AiUsageTokens(11, 7, 18))));

        public async Task<Result<AiProviderTokenBudget>> GetParseFoodTextTokenBudgetAsync(
            string text,
            string? userLanguage,
            string promptTemplate,
            CancellationToken cancellationToken) {
            if (BeforeVisionBudgetAsync is not null) {
                await BeforeVisionBudgetAsync(cancellationToken);
            }

            return ParseFoodTextBudgetResult ?? Result.Success(DefaultBudget);
        }

        public Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> ParseFoodTextAsync(
            string text,
            string? userLanguage,
            string promptTemplate,
            CancellationToken cancellationToken) =>
            Task.FromResult(ParseFoodTextResult ?? Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(
                CreateVisionModel(),
                "text-parse",
                "test-model",
                new AiUsageTokens(11, 7, 18))));

        public async Task<Result<AiProviderTokenBudget>> GetCalculateNutritionTokenBudgetAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            string promptTemplate,
            CancellationToken cancellationToken) {
            CalculateNutritionBudgetCalls++;
            if (BeforeCalculateNutritionBudgetAsync is not null) {
                await BeforeCalculateNutritionBudgetAsync(cancellationToken);
            }

            return CalculateNutritionBudgetResult ?? Result.Success(DefaultBudget);
        }

        public Task<Result<OpenAiFoodClientResponse<FoodNutritionModel>>> CalculateNutritionAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            string promptTemplate,
            CancellationToken cancellationToken) {
            CalculateNutritionCalls++;
            BeforeCalculateNutrition?.Invoke();
            return Task.FromResult(CalculateNutritionResult ?? Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(
                new FoodNutritionModel(52m, 0.3m, 0.2m, 14m, 2.4m, 0m, []),
                "nutrition",
                "test-model",
                new AiUsageTokens(11, 7, 18))));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAiQuotaRepository : IAiQuotaRepository {
        public AiQuotaReservationStatus ReserveStatus { get; init; } = AiQuotaReservationStatus.Acquired;
        public List<AiQuotaReservationRequest> Reservations { get; } = [];
        public List<AiQuotaUsage> Reconciliations { get; } = [];
        public List<string> Releases { get; } = [];
        public bool ReconcileTokenWasCanceled { get; private set; }

        public Task<AiQuotaReservationStatus> ReserveAsync(
            AiQuotaReservationRequest request,
            CancellationToken cancellationToken = default) {
            Reservations.Add(request);
            return Task.FromResult(ReserveStatus);
        }

        public Task ReconcileAsync(
            string requestId,
            AiQuotaUsage usage,
            CancellationToken cancellationToken = default) {
            ReconcileTokenWasCanceled = cancellationToken.IsCancellationRequested;
            Reconciliations.Add(usage);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(string requestId, CancellationToken cancellationToken = default) {
            Releases.Add(requestId);
            return Task.CompletedTask;
        }
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
        public override DateTimeOffset GetUtcNow() =>
            new(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    private static IAiPromptProvider CreateAiPromptProvider() {
        IAiPromptProvider provider = Substitute.For<IAiPromptProvider>();
        provider
            .GetPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<string>(0)));

        return provider;
    }
}
