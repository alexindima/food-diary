namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotOperationLease(Guid OperationId, Guid LeaseId, Guid UserId, long SecurityVersion,
    string Payload, string? Checkpoint, DateTime LeaseExpiresAtUtc, DateTime CreatedAtUtc = default);
