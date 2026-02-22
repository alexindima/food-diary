using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed class Role : AggregateRoot<RoleId>
{
    public string Name { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role()
    {
    }

    public static Role Create(string name)
    {
        var normalizedName = NormalizeRequiredName(name);

        var role = new Role
        {
            Id = RoleId.New(),
            Name = normalizedName
        };
        role.SetCreated();
        return role;
    }

    private static string NormalizeRequiredName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Role name is required.", nameof(value));
        }

        return value.Trim();
    }
}

