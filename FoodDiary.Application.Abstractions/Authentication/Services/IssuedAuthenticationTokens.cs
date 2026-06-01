namespace FoodDiary.Application.Abstractions.Authentication.Services;

public sealed record IssuedAuthenticationTokens(string AccessToken, string RefreshToken);
