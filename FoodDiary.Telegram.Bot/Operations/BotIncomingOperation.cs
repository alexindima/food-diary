namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotIncomingOperation(string Kind, long ChatId, long TelegramUserId, int MessageId,
    DateTime? OccurredAtUtc, string? Language, string? FileId = null, string? ContentType = null,
    string? Caption = null, string? CallbackId = null, int? AmountMl = null, Guid? MealOperationId = null, int? PeriodDays = null);
