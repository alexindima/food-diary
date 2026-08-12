namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserEmailVerificationDeliveryModel(
    Guid UserId,
    string Email,
    string? Language);
