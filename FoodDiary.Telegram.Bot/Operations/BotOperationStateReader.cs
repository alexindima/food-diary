using System.Text.Json;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotOperationStateReader {
    internal static BotIncomingOperation ReadIncoming(string payload) {
        BotIncomingOperation? incoming = JsonSerializer.Deserialize<BotIncomingOperation>(payload);
        if (incoming is null || incoming.Kind is not ("photo" or "water" or "meal-undo" or "statistics") ||
            incoming.TelegramUserId <= 0 || incoming.ChatId != incoming.TelegramUserId || incoming.MessageId <= 0) {
            throw new InvalidDataException("Invalid Telegram operation identity or kind.");
        }
        return incoming;
    }

    internal static BotPhotoCheckpoint ReadCheckpoint(string? payload, string kind) {
        BotPhotoCheckpoint checkpoint = payload is null ? new BotPhotoCheckpoint() :
            JsonSerializer.Deserialize<BotPhotoCheckpoint>(payload) ?? throw new InvalidDataException("Missing checkpoint.");
        bool valid = checkpoint.Stage is "received" or "failed" || (kind, checkpoint.Stage) switch {
            ("photo", "upload") => checkpoint.Upload is not null,
            ("photo", "image-ready") => checkpoint.ImageAssetId is not null,
            ("photo", "recognizing" or "recognition-ready") => checkpoint.RecognitionId is not null,
            ("photo", "meal-saved") => checkpoint.SavedMeal is not null,
            ("water", "water-saved") => checkpoint.WaterEntryId is not null,
            ("meal-undo", "undo-complete") => checkpoint.ErrorCode is not null,
            ("statistics", "statistics-ready") => checkpoint.Statistics is { CalendarDays: 1 or 7 } statistics &&
                statistics.Days is not null && statistics.Days.Count == statistics.CalendarDays,
            _ => false,
        };
        return valid ? checkpoint : throw new InvalidDataException("Invalid operation checkpoint stage.");
    }
}
