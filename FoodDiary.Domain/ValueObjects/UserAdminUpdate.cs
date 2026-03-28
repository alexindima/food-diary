using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.ValueObjects;

public sealed record UserAdminUpdate(
    bool? IsActive = null,
    UserAdminAccountUpdate Account = default,
    IReadOnlyCollection<Role>? Roles = null);
