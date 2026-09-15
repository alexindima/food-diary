namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record RegisterTelegramOperationHttpRequest(long UpdateId, long TelegramUserId, string Payload);
