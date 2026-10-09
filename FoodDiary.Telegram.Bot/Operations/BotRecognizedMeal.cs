namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotRecognizedMeal(BotOperationId OperationId, BotMealId MealId, DateTime UndoUntilUtc, bool Undone);
