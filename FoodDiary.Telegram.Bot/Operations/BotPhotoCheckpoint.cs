namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotPhotoCheckpoint(string Stage = "received", int UploadAttempt = 0,
    BotImageUpload? Upload = null, Guid? ImageAssetId = null, Guid? RecognitionId = null, string? ErrorCode = null,
    BotRecognizedMeal? SavedMeal = null, Guid? WaterEntryId = null, BotDiaryStatistics? Statistics = null,
    BotMealNutrition? Nutrition = null);
