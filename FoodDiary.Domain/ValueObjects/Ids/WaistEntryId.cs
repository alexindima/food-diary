using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WaistEntryId(Guid Value) : IEntityId<Guid> {
    public static WaistEntryId New() => new(Guid.NewGuid());
    public static WaistEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(WaistEntryId id) => id.Value;
    public static explicit operator WaistEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
