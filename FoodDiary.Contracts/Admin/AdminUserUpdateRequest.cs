namespace FoodDiary.Contracts.Admin;

public sealed record AdminUserUpdateRequest(
    bool? IsActive,
    string[] Roles
);
