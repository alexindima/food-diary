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
        Assert.StartsWith($"🔥 <b>{calories}</b>\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatHtml_TreatsItemLabelsAndUnitsAsPlainText() {
        var nutrition = new BotMealNutrition(10, 1, 1, 1,
            Items: [new BotMealNutrition.Item("<b>Apple</b>", NameLocal: null, Amount: 1, Unit: "a&b", Calories: 10)]);
        BotMealNutrition legacy = nutrition with { Items = null, ItemNames = ["<i>Apple</i>"] };
        Assert.Contains("&lt;b&gt;Apple&lt;/b&gt; · 1 a&amp;b · ~10 kcal", nutrition.Format(russian: false), StringComparison.Ordinal);
        Assert.Contains("&lt;i&gt;Apple&lt;/i&gt;", legacy.Format(russian: false), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("g", "г")]
    [InlineData("KG", "кг")]
    [InlineData("ml", "мл")]
    [InlineData("l", "л")]
    [InlineData("pcs", "шт.")]
    [InlineData("portion", "portion")]
    public void Format_UsesLocalizedItemNameAmountAndUnit(string unit, string localizedUnit) {
        var nutrition = new BotMealNutrition(20, 1, 1, 1,
            Items: [new BotMealNutrition.Item("Apple", "Яблоко", 1.25, unit, 20)]);

        Assert.Contains($"• Яблоко · 1,25 {localizedUnit} · ~20 ккал", nutrition.Format(russian: true), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, "g")]
    [InlineData(1, null)]
    public void Format_OmitsUnavailablePortion(double amount, string? unit) {
        var nutrition = new BotMealNutrition(20, 1, 1, 1,
            Items: [new BotMealNutrition.Item("Apple", NameLocal: null, amount, unit, 20)]);

        Assert.Contains("• Apple · ~20 ккал", nutrition.Format(russian: true), StringComparison.Ordinal);
    }
}
