namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotHydrationReceipt(BotOperationId OperationId, BotHydrationEntryId EntryId, DateTime TimestampUtc, int AmountMl);
