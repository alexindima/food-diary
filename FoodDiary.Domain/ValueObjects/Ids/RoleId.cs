using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RoleId(Guid Value) : IEntityId<Guid> {
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId Empty => new(Guid.Empty);

    public static implicit operator Guid(RoleId id) => id.Value;
    public static explicit operator RoleId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
