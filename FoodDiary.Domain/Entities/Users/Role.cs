using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed class Role : AggregateRoot<RoleId> {
    private const int NameMaxLength = 64;

    public string Name { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() {
    }

    public static Role Create(string name) {
        var normalizedName = NormalizeRequiredName(name);

        var role = new Role {
            Id = RoleId.New(),
            Name = normalizedName
        };
        role.SetCreated();
        return role;
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Role name is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Role name must be at most {NameMaxLength} characters.")
            : normalized;
    }
}
