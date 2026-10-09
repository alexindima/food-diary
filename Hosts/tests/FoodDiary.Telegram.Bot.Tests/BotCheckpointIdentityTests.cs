using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotCheckpointIdentityTests {
    [Fact]
    public void LegacyUploadCheckpoint_ReplaysWithoutChangingGuidJson() {
        var assetId = Guid.NewGuid();
        DateTime expiry = new(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        string legacy = JsonSerializer.Serialize(new {
            Stage = "upload",
            UploadAttempt = 2,
            Upload = new { UploadUrl = "https://storage.example/upload", FileUrl = "https://storage.example/file", ExpiresAtUtc = expiry, AssetId = assetId },
            ImageAssetId = assetId,
            RecognitionId = (Guid?)null,
            ErrorCode = (string?)null,
            SavedMeal = (object?)null,
            WaterEntryId = (Guid?)null,
            Statistics = (object?)null,
            Nutrition = (object?)null,
        });

        BotPhotoCheckpoint replayed = BotOperationStateReader.ReadCheckpoint(legacy, "photo");

        Assert.NotNull(replayed.Upload);
        Assert.Equal(assetId, replayed.Upload.AssetId.Value);
        Assert.Equal(expiry, replayed.Upload.ExpiresAtUtc);
        Assert.Equal(legacy, JsonSerializer.Serialize(replayed));
    }

    [Fact]
    public void LegacyMealCheckpoint_ReplaysWithExistingUndoAndEditEncodings() {
        var operationId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        DateTime now = new(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        string legacy = JsonSerializer.Serialize(new {
            Stage = "meal-saved",
            UploadAttempt = 0,
            Upload = (object?)null,
            ImageAssetId = (Guid?)null,
            RecognitionId = operationId,
            ErrorCode = (string?)null,
            SavedMeal = new { OperationId = operationId, MealId = mealId, UndoUntilUtc = now.AddHours(1), Undone = false },
            WaterEntryId = (Guid?)null,
            Statistics = (object?)null,
            Nutrition = (object?)null,
        });

        BotPhotoCheckpoint replayed = BotOperationStateReader.ReadCheckpoint(legacy, "photo");

        Assert.NotNull(replayed.SavedMeal);
        Assert.Equal(operationId, replayed.SavedMeal.OperationId.Value);
        Assert.Equal(mealId, replayed.SavedMeal.MealId.Value);
        Assert.Equal(legacy, JsonSerializer.Serialize(replayed));
        InlineKeyboardButton[] buttons = [.. BotMealActions.Create(replayed.SavedMeal, "https://diary.example", russian: false, now).InlineKeyboard.SelectMany(row => row)];
        Assert.Contains(buttons, button => string.Equals(button.CallbackData, $"meal:undo:{operationId:N}", StringComparison.Ordinal));
        Assert.Contains(buttons, button => string.Equals(button.WebApp?.Url, $"https://diary.example/meals/{mealId:D}/edit", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("\"not-a-guid\"")]
    [InlineData("null")]
    [InlineData("{}")]
    public void IdentityReaders_RetainGuidRejectionForInvalidJson(string json) {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Guid>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BotImageAssetId>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BotRecognitionId>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BotOperationId>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BotMealId>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BotHydrationEntryId>(json));
    }

    [Fact]
    public void EmptyGuidObservations_RetainTheirScalarEncoding() {
        string legacy = JsonSerializer.Serialize(Guid.Empty);
        Assert.Equal(legacy, JsonSerializer.Serialize(new BotImageAssetId(Guid.Empty)));
        Assert.Equal(Guid.Empty, JsonSerializer.Deserialize<BotImageAssetId>(legacy).Value);
    }
}
