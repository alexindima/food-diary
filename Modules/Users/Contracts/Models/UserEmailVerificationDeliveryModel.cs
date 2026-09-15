namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserEmailVerificationDeliveryModel(
    Guid UserId,
    string Email,
    string? Language);
