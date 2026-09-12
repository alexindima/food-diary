namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RegisterTelegramOperationHttpRequest(long UpdateId, long TelegramUserId, string Payload);
