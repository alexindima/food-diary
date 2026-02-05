using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

public sealed class UserRole
{
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole()
    {
    }

    public UserRole(UserId userId, RoleId roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
