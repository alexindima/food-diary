namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotRecognizedMeal(Guid OperationId, Guid MealId, DateTime UndoUntilUtc, bool Undone);
