using FoodDiary.Telegram.Bot.Images;
using Telegram.Bot.Types;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramImageSelectorTests {
    [Fact]
    public void Select_PhotoPicksLargestUsableResolution() {
        var message = new Message {
            Photo = [
                new PhotoSize { FileId = "small", Width = 100, Height = 100, FileSize = 1000 },
                new PhotoSize { FileId = "large", Width = 1000, Height = 1000, FileSize = 10000 },
                new PhotoSize { FileId = "oversized", Width = 2000, Height = 2000, FileSize = TelegramImageSelector.MaximumFileBytes + 1 },
            ],
        };
        TelegramImageSelection? result = TelegramImageSelector.Select(message, out string? error);
        Assert.Null(error);
        Assert.Equal("large", result?.FileId);
    }

    [Fact]
    public void Select_AlbumIsRejectedBeforeSelectingAnyPhoto() {
        var message = new Message { MediaGroupId = "album", Photo = [new PhotoSize { FileId = "photo", Width = 100, Height = 100 }] };
        Assert.Null(TelegramImageSelector.Select(message, out string? error));
        Assert.Equal("album", error);
    }

    [Theory]
    [InlineData("application/pdf", 100, "format")]
    [InlineData("image/png", 0, "size")]
    [InlineData("image/jpeg", TelegramImageSelector.MaximumFileBytes + 1, "size")]
    public void Select_UnsupportedDocumentIsRejected(string mime, long size, string expectedError) {
        var message = new Message { Document = new Document { FileId = "document", MimeType = mime, FileSize = size, FileName = "food.jpg" } };
        Assert.Null(TelegramImageSelector.Select(message, out string? error));
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void Select_UnknownSizeRemainsSubjectToDownloadLimit() {
        var message = new Message { Document = new Document { FileId = "document", MimeType = "image/png" } };
        TelegramImageSelection? result = TelegramImageSelector.Select(message, out string? error);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Null(result.DeclaredSizeBytes);
    }
}
