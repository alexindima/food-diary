namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserPasswordResetDeliveryModel(
    Guid UserId,
    string Email,
    string? Language);
