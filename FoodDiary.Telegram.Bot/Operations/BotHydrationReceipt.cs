namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotHydrationReceipt(Guid OperationId, Guid EntryId, DateTime TimestampUtc, int AmountMl);
