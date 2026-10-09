using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public sealed record TelegramOperationLease(TelegramOperationId OperationId, TelegramLeaseId LeaseId, UserId UserId, long SecurityVersion,
    string Payload, string? Checkpoint, DateTime LeaseExpiresAtUtc, DateTime CreatedAtUtc = default);
