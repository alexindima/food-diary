using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

public sealed class Role : AggregateRoot<RoleId>
{
    public string Name { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role()
    {
    }

    public static Role Create(string name)
    {
        var role = new Role
        {
            Id = RoleId.New(),
            Name = name
        };
        role.SetCreated();
        return role;
    }
}
