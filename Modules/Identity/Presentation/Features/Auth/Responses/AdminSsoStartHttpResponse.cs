namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record AdminSsoStartHttpResponse(
    string Code,
    DateTime ExpiresAtUtc);
