namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record LoginHttpRequest(
    string Email,
    string Password
);
