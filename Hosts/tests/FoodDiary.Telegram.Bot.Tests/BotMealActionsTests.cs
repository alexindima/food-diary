using FoodDiary.Telegram.Bot.Operations;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotMealActionsTests {
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void Create_OffersOnlyApplicableMealActions(bool undone, bool expired, bool hasUndo) {
        DateTime now = DateTime.UtcNow;
        var meal = new BotRecognizedMeal(new BotOperationId(Guid.NewGuid()), new BotMealId(Guid.NewGuid()), expired ? now : now.AddHours(1), undone);
        InlineKeyboardButton[][] rows = [.. BotMealActions.Create(meal, "https://diary.example/", russian: true, now)
            .InlineKeyboard.Select(row => row.ToArray())];
        Assert.Equal(2, rows.Length);
        Assert.Equal(hasUndo ? 2 : 1, rows[0].Length);
        Assert.Equal(undone ? "https://diary.example/meals" : $"https://diary.example/meals/{meal.MealId.Value:D}/edit", rows[0][0].WebApp?.Url);
        if (hasUndo) {
            Assert.Equal($"meal:undo:{meal.OperationId.Value:N}", rows[0][1].CallbackData);
        }
        InlineKeyboardButton summary = Assert.Single(rows[1]);
        Assert.Equal("Сводка дня", summary.Text);
        Assert.Equal("stats:today", summary.CallbackData);
    }
}
