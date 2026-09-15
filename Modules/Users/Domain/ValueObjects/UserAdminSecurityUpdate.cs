namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserAdminSecurityUpdate(
    bool? IsEmailConfirmed = null);
