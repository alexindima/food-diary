namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record ResendEmailVerificationHttpRequest(
    string? ClientOrigin = null
);
