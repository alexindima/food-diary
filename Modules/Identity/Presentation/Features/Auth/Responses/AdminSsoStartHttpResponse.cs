namespace FoodDiary.Presentation.Api.Features.Auth.Responses;

public sealed record AdminSsoStartHttpResponse(
    string Code,
    DateTime ExpiresAtUtc);
