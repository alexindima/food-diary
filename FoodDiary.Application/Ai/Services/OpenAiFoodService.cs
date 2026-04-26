using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class OpenAiFoodService(
    IOpenAiFoodClient openAiFoodClient,
    IAiUsageRepository aiUsageRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IAiPromptProvider aiPromptProvider)
    : IOpenAiFoodService {
    public async Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        CancellationToken cancellationToken) {
        const string operation = "vision";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        var promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken);
        var response = await openAiFoodClient.AnalyzeFoodImageAsync(
            imageUrl,
            userLanguage,
            description,
            promptTemplate,
            cancellationToken);
        if (response.IsFailure) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "text-parse";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodVisionModel>(quotaCheck.Error);
        }

        var promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken);
        var response = await openAiFoodClient.ParseFoodTextAsync(
            text,
            userLanguage,
            promptTemplate,
            cancellationToken);
        if (response.IsFailure) {
            return Result.Failure<FoodVisionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken);
        return Result.Success(response.Value.Value);
    }

    public async Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        CancellationToken cancellationToken) {
        const string operation = "nutrition";
        var quotaCheck = await EnsureMonthlyQuotaAsync(userId, operation, cancellationToken);
        if (quotaCheck.IsFailure) {
            return Result.Failure<FoodNutritionModel>(quotaCheck.Error);
        }

        var promptTemplate = await aiPromptProvider.GetPromptAsync(operation, cancellationToken);
        var response = await openAiFoodClient.CalculateNutritionAsync(items, promptTemplate, cancellationToken);
        if (response.IsFailure) {
            return Result.Failure<FoodNutritionModel>(response.Error);
        }

        await SaveUsageAsync(response.Value, userId, cancellationToken);
        return Result.Success(response.Value.Value);
    }

    private async Task<Result> EnsureMonthlyQuotaAsync(UserId userId, string operation, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(userId.Value));
        }

        var nowUtc = dateTimeProvider.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndUtc = monthStartUtc.AddMonths(1);
        var totals = await aiUsageRepository.GetUserTotalsAsync(userId, monthStartUtc, monthEndUtc, cancellationToken);

        if (totals.InputTokens >= user.AiInputTokenLimit || totals.OutputTokens >= user.AiOutputTokenLimit) {
            ApplicationAiTelemetry.RecordQuotaRejection(operation);
            return Result.Failure(Errors.Ai.QuotaExceeded());
        }

        return Result.Success();
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

        await aiUsageRepository.AddAsync(entity, cancellationToken);
    }
}
