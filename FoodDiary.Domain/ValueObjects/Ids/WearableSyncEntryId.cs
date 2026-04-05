using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WearableSyncEntryId(Guid Value) : IEntityId<Guid> {
    public static WearableSyncEntryId New() => new(Guid.NewGuid());
    public static WearableSyncEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(WearableSyncEntryId id) => id.Value;
    public static explicit operator WearableSyncEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
