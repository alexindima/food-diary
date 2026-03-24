namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RestoreAccountHttpRequest(
    string Email,
    string Password
);
