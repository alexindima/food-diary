using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace FoodDiary.Telegram.Bot.Images;

internal sealed class TelegramImageDownloader(ITelegramBotClient botClient) {
    internal async Task<byte[]> DownloadAsync(TelegramImageSelection image, CancellationToken cancellationToken) {
        TGFile file = await botClient.GetFile(image.FileId, cancellationToken).ConfigureAwait(false);
        if (file.FileSize is <= 0 or > TelegramImageSelector.MaximumFileBytes || string.IsNullOrWhiteSpace(file.FilePath)) {
            throw new InvalidDataException("Telegram image metadata is invalid.");
        }
        var stream = new BoundedImageStream((int)TelegramImageSelector.MaximumFileBytes);
        await using (stream.ConfigureAwait(false)) {
            try {
                await botClient.DownloadFile(file.FilePath, stream, cancellationToken).ConfigureAwait(false);
            } catch (RequestException error) when (error.InnerException is InvalidDataException) {
                throw new InvalidDataException("Telegram image exceeds the download limit.", error);
            }
            byte[] content = stream.ToArray();
            if (!HasExpectedSignature(content, image.ContentType)) {
                throw new InvalidDataException("Telegram file does not match its image type.");
            }
            return content;
        }
    }

    internal static bool HasExpectedSignature(ReadOnlySpan<byte> content, string contentType) => contentType switch {
        "image/jpeg" => content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff,
        "image/png" => content.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/webp" => content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false,
    };
}
