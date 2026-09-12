namespace FoodDiary.Presentation.Api.Features.Auth.Responses;

public sealed record TelegramOperationLeaseHttpResponse(Guid OperationId, Guid LeaseId, Guid UserId, long SecurityVersion,
    string Payload, string? Checkpoint, DateTime LeaseExpiresAtUtc, DateTime CreatedAtUtc = default);
