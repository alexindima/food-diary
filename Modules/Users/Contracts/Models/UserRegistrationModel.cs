namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserRegistrationModel(
    string Email,
    string Password,
    string? Language,
    string EmailVerificationToken,
    DateTime EmailVerificationExpiresAtUtc,
    DateTime RegisteredAtUtc);
