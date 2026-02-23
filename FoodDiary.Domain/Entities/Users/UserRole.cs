using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed class UserRole {
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() {
    }

    public UserRole(UserId userId, RoleId roleId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (roleId == RoleId.Empty) {
            throw new ArgumentException("RoleId is required.", nameof(roleId));
        }

        UserId = userId;
        RoleId = roleId;
    }
}
