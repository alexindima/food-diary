namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAdminSecurityUpdate(
    bool? IsEmailConfirmed = null);
