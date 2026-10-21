using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotCheckpointIdentityTests {
    [Fact]
    public void CopiedIdentities_PreserveTheOriginalAndTheirScalarFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        (object Original, object Copy)[] examples = [
            (BotOperationId.Empty, BotOperationId.Empty with { Value = value }),
            (new BotImageAssetId(Guid.Empty), new BotImageAssetId(Guid.Empty) with { Value = value }),
            (new BotRecognitionId(Guid.Empty), new BotRecognitionId(Guid.Empty) with { Value = value }),
            (new BotMealId(Guid.Empty), new BotMealId(Guid.Empty) with { Value = value }),
            (new BotHydrationEntryId(Guid.Empty), new BotHydrationEntryId(Guid.Empty) with { Value = value }),
            (new BotLeaseId(Guid.Empty), new BotLeaseId(Guid.Empty) with { Value = value }),
            (new BotUserId(Guid.Empty), new BotUserId(Guid.Empty) with { Value = value }),
        ];

        Assert.All(examples, example => Assert.Multiple(
            () => Assert.Equal(Guid.Empty.ToString(), example.Original.ToString()),
            () => Assert.Equal(value.ToString(), example.Copy.ToString()),
            () => Assert.NotEqual(example.Original, example.Copy)));
    }

    [Theory]
    [InlineData(typeof(BotOperationId))]
    [InlineData(typeof(BotImageAssetId))]
    [InlineData(typeof(BotRecognitionId))]
    [InlineData(typeof(BotMealId))]
    [InlineData(typeof(BotHydrationEntryId))]
    public void IdentityReaders_RoundTripNonEmptyGuidsWithoutObjectWrappers(Type type) {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        string scalar = JsonSerializer.Serialize(value);

        object? identity = JsonSerializer.Deserialize(scalar, type);
        Assert.NotNull(identity);
        Assert.IsType(type, identity);

        Assert.Multiple(
            () => Assert.Equal(scalar, JsonSerializer.Serialize(identity, type)),
            () => Assert.Equal(value.ToString(), identity.ToString()));
    }

    [Fact]
    public void IdentityConverter_RefusesUnrelatedTypes() {
        var factory = new BotGuidIdJsonConverterFactory();
        Assert.False(factory.CanConvert(typeof(Guid)));
        Assert.Throws<NotSupportedException>(() => factory.CreateConverter(typeof(Guid), JsonSerializerOptions.Default));
    }

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
