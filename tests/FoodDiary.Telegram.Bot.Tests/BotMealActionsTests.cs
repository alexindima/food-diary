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
        var meal = new BotRecognizedMeal(Guid.NewGuid(), Guid.NewGuid(), expired ? now : now.AddHours(1), undone);
        InlineKeyboardButton[] buttons = [.. BotMealActions.Create(meal, "https://diary.example/", russian: true, now).InlineKeyboard.SelectMany(row => row)];
        Assert.Equal(hasUndo, buttons.Any(button => string.Equals(button.CallbackData, $"meal:undo:{meal.OperationId:N}", StringComparison.Ordinal)));
        Assert.Contains(buttons, button => string.Equals(button.CallbackData, "stats:today", StringComparison.Ordinal));
        Assert.Contains(buttons, button => string.Equals(button.WebApp?.Url, undone ? "https://diary.example/meals" : $"https://diary.example/meals/{meal.MealId:D}/edit", StringComparison.Ordinal));
    }
}
