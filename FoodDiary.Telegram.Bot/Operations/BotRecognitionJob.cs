namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotRecognitionJob(Guid Id, Guid ImageAssetId, string Status, string? ErrorCode, string? NutritionErrorCode,
    BotMealNutrition? Nutrition = null);
