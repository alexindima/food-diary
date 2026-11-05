using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotMealNutritionTests {
    [Theory]
    [InlineData(true, "~123,4 ккал")]
    [InlineData(false, "~123.4 kcal")]
    public void Checkpoint_RetainsEstimateForDeliveryAfterRestart(bool russian, string calories) {
        var original = new BotPhotoCheckpoint(Stage: "meal-saved", Nutrition: new BotMealNutrition(123.4m, 5m, 6m, 7m));
        BotPhotoCheckpoint? restored = JsonSerializer.Deserialize<BotPhotoCheckpoint>(JsonSerializer.Serialize(original));
        Assert.NotNull(restored?.Nutrition);
        string text = restored.Nutrition.Format(russian);
        Assert.StartsWith($"<b>{calories}</b>\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatHtml_TreatsItemLabelsAndUnitsAsPlainText() {
        var nutrition = new BotMealNutrition(10, 1, 1, 1,
            Items: [new BotMealNutrition.Item("<b>Apple</b>", NameLocal: null, Amount: 1, Unit: "a&b", Calories: 10)]);
        BotMealNutrition legacy = nutrition with { Items = null, ItemNames = ["<i>Apple</i>"] };
        Assert.Contains("&lt;b&gt;Apple&lt;/b&gt; · 1 a&amp;b · ~10 kcal", nutrition.Format(russian: false), StringComparison.Ordinal);
        Assert.Contains("&lt;i&gt;Apple&lt;/i&gt;", legacy.Format(russian: false), StringComparison.Ordinal);
    }
}
