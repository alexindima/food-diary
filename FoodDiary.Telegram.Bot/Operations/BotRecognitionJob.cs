namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotRecognitionJob(BotRecognitionId Id, BotImageAssetId ImageAssetId, string Status, string? ErrorCode, string? NutritionErrorCode,
    BotMealNutrition? Nutrition = null);
