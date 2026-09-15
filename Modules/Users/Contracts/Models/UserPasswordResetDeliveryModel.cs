namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserPasswordResetDeliveryModel(
    Guid UserId,
    string Email,
    string? Language);
