namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record VerifyEmailHttpRequest(
    Guid UserId,
    string Token
);
