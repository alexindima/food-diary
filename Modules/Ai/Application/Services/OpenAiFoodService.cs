using FoodDiary.Modules.Ai.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed partial class OpenAiFoodService(
    IOpenAiFoodClient openAiFoodClient,
    IAiQuotaRepository aiQuotaRepository,
    IUserAiProfileReadService userProfileReadService,
    TimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider,
    TimeSpan? overallOperationTimeout = null,
    IRecipeSourceReader? recipeSourceReader = null,
    IRecipeVideoProcessor? recipeVideoProcessor = null)
    : IOpenAiFoodService {
    private static readonly TimeSpan DefaultOverallOperationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PersistenceTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _overallOperationTimeout = overallOperationTimeout is null or { Ticks: > 0 }
        ? overallOperationTimeout ?? DefaultOverallOperationTimeout
        : throw new ArgumentOutOfRangeException(nameof(overallOperationTimeout));

    public async Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        UserId userId,
        string? description,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null, ProductImageAnalysis? product = null) {
        const string operation = "vision";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(contextResult.Error);
            }

            string promptTemplate = promptOverride?.PromptText ?? await aiPromptProvider.GetPromptAsync(product is null ? operation : "product-label", contextResult.Value.Language, deadline.Token);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetAnalyzeFoodImageTokenBudgetAsync(
                imageUrl,
                promptOverride?.Locale ?? contextResult.Value.Language,
                description,
                promptTemplate,
                deadline.Token, product);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token);
            if (reservation.IsFailure) {
                return Result.Failure<FoodVisionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.AnalyzeFoodImageAsync(
                imageUrl,
                promptOverride?.Locale ?? contextResult.Value.Language,
                description,
                promptTemplate,
                deadline.Token, product);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodVisionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value);
            return Result.Success(response.Value.Value);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested) {
            return Result.Failure<FoodVisionModel>(AiErrors.OpenAiFailed("OpenAI operation deadline expired."));
        }
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null) {
        const string operation = "text-parse";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(contextResult.Error);
            }

            string promptTemplate = promptOverride?.PromptText ?? await aiPromptProvider.GetPromptAsync(operation, contextResult.Value.Language, deadline.Token);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetParseFoodTextTokenBudgetAsync(
                text,
                promptOverride?.Locale ?? contextResult.Value.Language,
                promptTemplate,
                deadline.Token);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token);
            if (reservation.IsFailure) {
                return Result.Failure<FoodVisionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.ParseFoodTextAsync(
                text,
                promptOverride?.Locale ?? contextResult.Value.Language,
                promptTemplate,
                deadline.Token);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodVisionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value);
            return Result.Success(response.Value.Value);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested) {
            return Result.Failure<FoodVisionModel>(AiErrors.OpenAiFailed("OpenAI operation deadline expired."));
        }
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null) {
        const string operation = "nutrition";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodNutritionModel>(contextResult.Error);
            }

            string promptTemplate = promptOverride?.PromptText ?? await aiPromptProvider.GetPromptAsync(operation, contextResult.Value.Language, deadline.Token);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetCalculateNutritionTokenBudgetAsync(
                items,
                promptTemplate,
                deadline.Token);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodNutritionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token);
            if (reservation.IsFailure) {
                return Result.Failure<FoodNutritionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodNutritionModel>> response = await openAiFoodClient.CalculateNutritionAsync(
                items,
                promptTemplate,
                deadline.Token);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodNutritionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value);
            return Result.Success(response.Value.Value);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested) {
            return Result.Failure<FoodNutritionModel>(AiErrors.OpenAiFailed("OpenAI operation deadline expired."));
        }
    }

    private CancellationTokenSource CreateOperationDeadline(CancellationToken cancellationToken) {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_overallOperationTimeout);
        return deadline;
    }

    private async Task<Result<UserAiProfileModel>> GetUserContextAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserAiProfileModel> contextResult = await userProfileReadService
            .GetAiProfileAsync(userId, cancellationToken);
        if (contextResult.IsFailure) {
            return contextResult;
        }

        if (!contextResult.Value.HasAcceptedAiConsent) {
            return Result.Failure<UserAiProfileModel>(AiErrors.ConsentRequired());
        }

        return AiQuotaWindow.HasExpiredTrialOnlyAccess(contextResult.Value, dateTimeProvider.GetUtcNow().UtcDateTime)
            ? Result.Failure<UserAiProfileModel>(AiErrors.TrialEnded())
            : contextResult;
    }

    private async Task<Result> ReserveAsync(
        string requestId,
        UserId userId,
        string operation,
        UserAiProfileModel context,
        AiProviderTokenBudget budget,
        CancellationToken cancellationToken) {
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        var window = AiQuotaWindow.Resolve(context, nowUtc);
        if (AiQuotaWindow.HasExpiredTrialOnlyAccess(context, nowUtc)) {
            return Result.Failure(AiErrors.TrialEnded());
        }

        AiQuotaReservationStatus status = await aiQuotaRepository.ReserveAsync(
            new AiQuotaReservationRequest(
                requestId,
                userId,
                window.ReservationKeyUtc,
                operation,
                budget.InputTokens,
                budget.MaximumOutputTokens,
                window.InputLimit,
                window.OutputLimit,
                nowUtc.Add(ReservationLifetime),
                window.IsTrial ? window.StartUtc : null),
            cancellationToken);
        ApplicationAiTelemetry.RecordQuotaReservation(operation, status);

        if (status == AiQuotaReservationStatus.Acquired) {
            return Result.Success();
        }

        ApplicationAiTelemetry.RecordQuotaRejection(operation);
        return Result.Failure(window.IsTrial && status == AiQuotaReservationStatus.QuotaExceeded
            ? AiErrors.TrialQuotaExceeded()
            : AiErrors.QuotaExceeded());
    }

    private async Task ReconcileAsync<T>(
        string requestId,
        OpenAiFoodClientResponse<T> response,
        AiProviderTokenBudget budget) {
        AiTokenUsage usage = response.Usage ?? AiTokenUsage.FromCounts(
            checked((int)budget.InputTokens),
            checked((int)budget.MaximumOutputTokens),
            checked((int)(budget.InputTokens + budget.MaximumOutputTokens)));
        using var timeout = new CancellationTokenSource(PersistenceTimeout);
        await aiQuotaRepository.ReconcileAsync(
            requestId,
            new AiQuotaUsage(
                response.Operation,
                response.Model,
                usage),
            timeout.Token);
        ApplicationAiTelemetry.RecordQuotaReconciliation(response.Operation, response.Usage is null ? "estimated" : "actual");
    }

}
