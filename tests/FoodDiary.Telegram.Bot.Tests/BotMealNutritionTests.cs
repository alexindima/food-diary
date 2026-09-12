using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotMealNutritionTests {
    [Theory]
    [InlineData(true, "Оценка по фото", "123.4 ккал")]
    [InlineData(false, "Photo estimate", "123.4 kcal")]
    public void Checkpoint_RetainsEstimateForDeliveryAfterRestart(bool russian, string heading, string calories) {
        var original = new BotPhotoCheckpoint(Stage: "meal-saved", Nutrition: new BotMealNutrition(123.4m, 5m, 6m, 7m));
        BotPhotoCheckpoint? restored = JsonSerializer.Deserialize<BotPhotoCheckpoint>(JsonSerializer.Serialize(original));
        Assert.NotNull(restored?.Nutrition);
        string text = restored.Nutrition.Format(russian);
        Assert.Contains(heading, text, StringComparison.Ordinal);
        Assert.Contains(calories, text, StringComparison.Ordinal);
    }
}
