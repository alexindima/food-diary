using Telegram.Bot.Types;

namespace FoodDiary.Telegram.Bot.Images;

internal static class TelegramImageSelector {
    internal const long MaximumFileBytes = 20 * 1024 * 1024;

    internal static TelegramImageSelection? Select(Message message, out string? error) {
        error = null;
        if (message.MediaGroupId is not null) {
            error = "album";
            return null;
        }
        if (message.Photo is { Length: > 0 } photos) {
            PhotoSize? selected = photos
                .Where(photo => photo.Width > 0 && photo.Height > 0 && !string.IsNullOrWhiteSpace(photo.FileId) &&
                    (photo.FileSize is null || photo.FileSize is > 0 and <= MaximumFileBytes))
                .OrderByDescending(photo => (long)photo.Width * photo.Height)
                .FirstOrDefault();
            if (selected is null) {
                error = "size";
                return null;
            }
            return new TelegramImageSelection(selected.FileId, "image/jpeg", selected.FileSize);
        }
        if (message.Document is not { } document) {
            return null;
        }
        string? contentType = document.MimeType?.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp")) {
            error = "format";
            return null;
        }
        if (string.IsNullOrWhiteSpace(document.FileId) || document.FileSize is <= 0 or > MaximumFileBytes) {
            error = "size";
            return null;
        }
        return new TelegramImageSelection(document.FileId, contentType, document.FileSize);
    }
}
