namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record TelegramOperationLease(Guid OperationId, Guid LeaseId, Guid UserId, long SecurityVersion,
    string Payload, string? Checkpoint, DateTime LeaseExpiresAtUtc, DateTime CreatedAtUtc = default);
