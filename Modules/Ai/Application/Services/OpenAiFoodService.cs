using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed class OpenAiFoodService(
    IOpenAiFoodClient openAiFoodClient,
    IAiQuotaRepository aiQuotaRepository,
    IUserAiProfileReadService userProfileReadService,
    TimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider,
    TimeSpan? overallOperationTimeout = null)
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
        CancellationToken cancellationToken) {
        const string operation = "vision";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token).ConfigureAwait(false);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(contextResult.Error);
            }

            string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, contextResult.Value.Language, deadline.Token).ConfigureAwait(false);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetAnalyzeFoodImageTokenBudgetAsync(
                imageUrl,
                contextResult.Value.Language,
                description,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token).ConfigureAwait(false);
            if (reservation.IsFailure) {
                return Result.Failure<FoodVisionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.AnalyzeFoodImageAsync(
                imageUrl,
                contextResult.Value.Language,
                description,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodVisionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
            return Result.Success(response.Value.Value);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested) {
            return Result.Failure<FoodVisionModel>(AiErrors.OpenAiFailed("OpenAI operation deadline expired."));
        }
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token).ConfigureAwait(false);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(contextResult.Error);
            }

            string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, contextResult.Value.Language, deadline.Token).ConfigureAwait(false);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetParseFoodTextTokenBudgetAsync(
                text,
                contextResult.Value.Language,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodVisionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token).ConfigureAwait(false);
            if (reservation.IsFailure) {
                return Result.Failure<FoodVisionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.ParseFoodTextAsync(
                text,
                contextResult.Value.Language,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodVisionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
            return Result.Success(response.Value.Value);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested) {
            return Result.Failure<FoodVisionModel>(AiErrors.OpenAiFailed("OpenAI operation deadline expired."));
        }
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> contextResult = await GetUserContextAsync(userId, deadline.Token).ConfigureAwait(false);
            if (contextResult.IsFailure) {
                return Result.Failure<FoodNutritionModel>(contextResult.Error);
            }

            string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, contextResult.Value.Language, deadline.Token).ConfigureAwait(false);
            Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetCalculateNutritionTokenBudgetAsync(
                items,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (budgetResult.IsFailure) {
                return Result.Failure<FoodNutritionModel>(budgetResult.Error);
            }

            Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, deadline.Token).ConfigureAwait(false);
            if (reservation.IsFailure) {
                return Result.Failure<FoodNutritionModel>(reservation.Error);
            }

            Result<OpenAiFoodClientResponse<FoodNutritionModel>> response = await openAiFoodClient.CalculateNutritionAsync(
                items,
                promptTemplate,
                deadline.Token).ConfigureAwait(false);
            if (response.IsFailure) {
                // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
                return Result.Failure<FoodNutritionModel>(response.Error);
            }

            await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
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
            .GetAiProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return contextResult;
        }

        return contextResult.Value.HasAcceptedAiConsent
            ? contextResult
            : Result.Failure<UserAiProfileModel>(AiErrors.ConsentRequired());
    }

    private async Task<Result> ReserveAsync(
        string requestId,
        UserId userId,
        string operation,
        UserAiProfileModel context,
        AiProviderTokenBudget budget,
        CancellationToken cancellationToken) {
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        AiQuotaReservationStatus status = await aiQuotaRepository.ReserveAsync(
            new AiQuotaReservationRequest(
                requestId,
                userId,
                monthStartUtc,
                operation,
                budget.InputTokens,
                budget.MaximumOutputTokens,
                context.InputTokenLimit,
                context.OutputTokenLimit,
                nowUtc.Add(ReservationLifetime)),
            cancellationToken).ConfigureAwait(false);
        ApplicationAiTelemetry.RecordQuotaReservation(operation, status);

        if (status == AiQuotaReservationStatus.Acquired) {
            return Result.Success();
        }

        ApplicationAiTelemetry.RecordQuotaRejection(operation);
        return Result.Failure(AiErrors.QuotaExceeded());
    }

    private async Task ReconcileAsync<T>(
        string requestId,
        OpenAiFoodClientResponse<T> response,
        AiProviderTokenBudget budget) {
        AiUsageTokens usage = response.Usage ?? new AiUsageTokens(
            checked((int)budget.InputTokens),
            checked((int)budget.MaximumOutputTokens),
            checked((int)(budget.InputTokens + budget.MaximumOutputTokens)));
        using var timeout = new CancellationTokenSource(PersistenceTimeout);
        await aiQuotaRepository.ReconcileAsync(
            requestId,
            new AiQuotaUsage(
                response.Operation,
                response.Model,
                usage.InputTokens,
                usage.OutputTokens,
                usage.TotalTokens),
            timeout.Token).ConfigureAwait(false);
        ApplicationAiTelemetry.RecordQuotaReconciliation(response.Operation, response.Usage is null ? "estimated" : "actual");
    }

}
