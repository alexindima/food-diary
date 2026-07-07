using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

internal static class ConsumptionAiSessionAppender {
    public static async Task<Result> AddAsync(
        Meal meal,
        IEnumerable<ConsumptionAiSessionInput> sessions,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        TimeProvider dateTimeProvider,
        CancellationToken cancellationToken) {
        foreach (ConsumptionAiSessionInput session in sessions) {
            Result sessionResult = await AddSessionAsync(
                meal,
                session,
                userId,
                imageAssetAccessService,
                dateTimeProvider,
                cancellationToken).ConfigureAwait(false);
            if (sessionResult.IsFailure) {
                return sessionResult;
            }
        }

        return Result.Success();
    }

    private static async Task<Result> AddSessionAsync(
        Meal meal,
        ConsumptionAiSessionInput session,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        TimeProvider dateTimeProvider,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> sessionImageAssetIdResult = ImageAssetIdParser.ParseOptional(session.ImageAssetId, nameof(session.ImageAssetId));
        if (sessionImageAssetIdResult.IsFailure) {
            return sessionImageAssetIdResult;
        }

        Result<ImageAsset?> sessionImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            sessionImageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (sessionImageAssetResult.IsFailure) {
            return sessionImageAssetResult;
        }

        Result<List<MealAiItemData>> sessionItemsResult = CreateAiSessionItems(session);
        if (sessionItemsResult.IsFailure) {
            return sessionItemsResult;
        }

        if (!TryParseAiRecognitionSource(session.Source, out AiRecognitionSource sessionSource)) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(session.Source), "Unknown AI recognition source value."));
        }

        DateTime recognizedAtUtc = session.RecognizedAtUtc ?? dateTimeProvider.GetUtcNow().UtcDateTime;
        if (recognizedAtUtc.Kind == DateTimeKind.Unspecified) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(session.RecognizedAtUtc), "RecognizedAtUtc timestamp kind must be specified."));
        }

        if (session.Notes is { Length: > 2048 }) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(session.Notes), "Notes must be at most 2048 characters."));
        }

        meal.AddAiSession(
            sessionImageAssetIdResult.Value,
            sessionSource,
            recognizedAtUtc,
            session.Notes,
            sessionItemsResult.Value);

        return Result.Success();
    }

    private static bool TryParseAiRecognitionSource(string? source, out AiRecognitionSource result) {
        if (!string.IsNullOrWhiteSpace(source)) {
            return EnumValueParser.TryParse(source, out result);
        }

        result = AiRecognitionSource.Text;
        return true;
    }

    private static Result<List<MealAiItemData>> CreateAiSessionItems(ConsumptionAiSessionInput session) {
        var items = new List<MealAiItemData>(session.Items.Count);
        foreach (ConsumptionAiItemInput aiItem in session.Items) {
            if (!TryParseAiItemResolution(aiItem.Resolution, out MealAiItemResolution resolution)) {
                return Result.Failure<List<MealAiItemData>>(
                    Errors.Validation.Invalid(nameof(aiItem.Resolution), "Unknown AI item resolution value."));
            }

            if (!MealAiItemData.TryCreate(
                    aiItem.NameEn,
                    aiItem.NameLocal,
                    aiItem.Amount,
                    aiItem.Unit,
                    aiItem.Calories,
                    aiItem.Proteins,
                    aiItem.Fats,
                    aiItem.Carbs,
                    aiItem.Fiber,
                    aiItem.Alcohol,
                    aiItem.Confidence ?? 1,
                    resolution,
                    out MealAiItemData? data,
                    out string? error)) {
                return Result.Failure<List<MealAiItemData>>(
                    Errors.Validation.Invalid("AiSessions", error ?? "AI item is invalid."));
            }

            items.Add(data!);
        }

        return Result.Success(items);
    }

    private static bool TryParseAiItemResolution(string? resolution, out MealAiItemResolution result) {
        if (!string.IsNullOrWhiteSpace(resolution)) {
            return EnumValueParser.TryParse(resolution, out result);
        }

        result = MealAiItemResolution.Accepted;
        return true;
    }
}
