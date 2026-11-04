using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotPhotoCheckpoint(string Stage = "received", int UploadAttempt = 0,
    BotImageUpload? Upload = null, Guid? ImageAssetId = null, Guid? RecognitionId = null, string? ErrorCode = null,
    BotRecognizedMeal? SavedMeal = null, Guid? WaterEntryId = null, BotDiaryStatistics? Statistics = null,
    BotMealNutrition? Nutrition = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool PhotoPremiumApproved = false,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ProgressMessageId = null);
