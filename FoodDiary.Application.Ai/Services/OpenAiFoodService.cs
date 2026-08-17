using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class OpenAiFoodService(
    IOpenAiFoodClient openAiFoodClient,
    IAiQuotaRepository aiQuotaRepository,
    IAiUserContextService aiUserContextService,
    TimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider)
    : IOpenAiFoodService {
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PersistenceTimeout = TimeSpan.FromSeconds(5);

    public async Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        string requestId,
        CancellationToken cancellationToken) {
        const string operation = "vision";
        Result<AiUserContext> contextResult = await GetUserContextAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(contextResult.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetAnalyzeFoodImageTokenBudgetAsync(
            imageUrl,
            userLanguage,
            description,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (budgetResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(budgetResult.Error);
        }

        Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure) {
            return Result.Failure<FoodVisionModel>(reservation.Error);
        }

        Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.AnalyzeFoodImageAsync(
            imageUrl,
            userLanguage,
            description,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        Result<AiUserContext> contextResult = await GetUserContextAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(contextResult.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetParseFoodTextTokenBudgetAsync(
            text,
            userLanguage,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (budgetResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(budgetResult.Error);
        }

        Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure) {
            return Result.Failure<FoodVisionModel>(reservation.Error);
        }

        Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.ParseFoodTextAsync(
            text,
            userLanguage,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        Result<AiUserContext> contextResult = await GetUserContextAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodNutritionModel>(contextResult.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<AiProviderTokenBudget> budgetResult = await openAiFoodClient.GetCalculateNutritionTokenBudgetAsync(
            items,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (budgetResult.IsFailure) {
            return Result.Failure<FoodNutritionModel>(budgetResult.Error);
        }

        Result reservation = await ReserveAsync(requestId, userId, operation, contextResult.Value, budgetResult.Value, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure) {
            return Result.Failure<FoodNutritionModel>(reservation.Error);
        }

        Result<OpenAiFoodClientResponse<FoodNutritionModel>> response = await openAiFoodClient.CalculateNutritionAsync(
            items,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            // A transport/provider failure does not prove the request was not processed; expiry charges the reservation conservatively.
            return Result.Failure<FoodNutritionModel>(response.Error);
        }

        await ReconcileAsync(requestId, response.Value, budgetResult.Value).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    private Task<Result<AiUserContext>> GetUserContextAsync(UserId userId, CancellationToken cancellationToken) =>
        aiUserContextService.GetAsync(userId, cancellationToken);

    private async Task<Result> ReserveAsync(
        string requestId,
        UserId userId,
        string operation,
        AiUserContext context,
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
        return Result.Failure(Errors.Ai.QuotaExceeded());
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
