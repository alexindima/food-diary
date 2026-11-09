namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record ResumeTelegramOperationHttpRequest(long TelegramUserId, string ExpectedCheckpoint, string Checkpoint);
