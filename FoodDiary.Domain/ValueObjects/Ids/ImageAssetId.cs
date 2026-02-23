using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ImageAssetId(Guid Value) : IEntityId<Guid> {
    public static ImageAssetId New() => new(Guid.NewGuid());
    public static ImageAssetId Empty => new(Guid.Empty);

    public static implicit operator Guid(ImageAssetId id) => id.Value;
    public static explicit operator ImageAssetId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
