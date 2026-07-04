using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class OpenAiFoodService(
    IOpenAiFoodClient openAiFoodClient,
    IAiUsageWriteRepository aiUsageRepository,
    IAiUserContextService aiUserContextService,
    TimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider)
    : IOpenAiFoodService {
    public async Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        CancellationToken cancellationToken) {
        const string operation = "vision";
        Result quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken).ConfigureAwait(false);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.AnalyzeFoodImageAsync(
            imageUrl,
            userLanguage,
            description,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        Result quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken).ConfigureAwait(false);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<OpenAiFoodClientResponse<FoodVisionModel>> response = await openAiFoodClient.ParseFoodTextAsync(
            text,
            userLanguage,
            promptTemplate,
            cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        Result quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken).ConfigureAwait(false);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodNutritionModel>(quotaCheck.Error);
        }

        string promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken).ConfigureAwait(false);
        Result<OpenAiFoodClientResponse<FoodNutritionModel>> response = await openAiFoodClient.CalculateNutritionAsync(items, promptTemplate, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            return Result.Failure<FoodNutritionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(response.Value.Value);
    }

    private async Task<Result> EnsureMonthlyQuotaAsync(UserId userId, string operation, CancellationToken cancellationToken) {
        Result<AiUserContext> contextResult = await aiUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure(contextResult.Error);
        }

        AiUserContext context = contextResult.Value;
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime monthEndUtc = monthStartUtc.AddMonths(1);
        AiUsageTotals totals = await aiUsageRepository.GetUserTotalsAsync(userId, monthStartUtc, monthEndUtc, cancellationToken).ConfigureAwait(false);

        if (totals.InputTokens < context.InputTokenLimit && totals.OutputTokens < context.OutputTokenLimit) {
            return Result.Success();
        }

        ApplicationAiTelemetry.RecordQuotaRejection(operation);
        return Result.Failure(Errors.Ai.QuotaExceeded());

    }

    private async Task SaveUsageAsync<T>(
        OpenAiFoodClientResponse<T> response,
        UserId userId,
        CancellationToken cancellationToken) {
        if (response.Usage is null) {
            return;
        }

        var entity = AiUsage.Create(
            userId,
            response.Operation,
            response.Model,
            response.Usage.Value.InputTokens,
            response.Usage.Value.OutputTokens,
            response.Usage.Value.TotalTokens);

        await aiUsageRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }
}
