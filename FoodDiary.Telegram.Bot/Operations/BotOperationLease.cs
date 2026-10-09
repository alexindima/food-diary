namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotOperationLease(BotOperationId OperationId, BotLeaseId LeaseId, BotUserId UserId, long SecurityVersion,
    string Payload, string? Checkpoint, DateTime LeaseExpiresAtUtc, DateTime CreatedAtUtc = default);
