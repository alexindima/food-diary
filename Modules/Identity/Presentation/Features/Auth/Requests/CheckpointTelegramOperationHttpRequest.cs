namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record CheckpointTelegramOperationHttpRequest(Guid LeaseId, string Checkpoint, bool Completed, DateTime NextAttemptAtUtc);
