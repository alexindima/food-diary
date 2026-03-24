namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record ConfirmPasswordResetHttpRequest(
    Guid UserId,
    string Token,
    string NewPassword
);
