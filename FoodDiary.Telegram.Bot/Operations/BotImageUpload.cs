namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotImageUpload(string UploadUrl, string FileUrl, DateTime ExpiresAtUtc, Guid AssetId);
