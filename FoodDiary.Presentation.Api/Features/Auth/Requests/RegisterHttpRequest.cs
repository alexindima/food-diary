namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RegisterHttpRequest(
    string Email,
    string Password,
    string? Language,
    string? ClientOrigin = null
);
