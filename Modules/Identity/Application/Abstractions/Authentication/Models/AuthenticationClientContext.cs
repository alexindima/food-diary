namespace FoodDiary.Application.Abstractions.Authentication.Models;

public sealed record AuthenticationClientContext(
    string AuthProvider,
    string? IpAddress,
    string? UserAgent);
