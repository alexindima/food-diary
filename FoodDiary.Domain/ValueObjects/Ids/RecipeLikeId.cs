using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecipeLikeId(Guid Value) : IEntityId<Guid> {
    public static RecipeLikeId New() => new(Guid.NewGuid());
    public static RecipeLikeId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeLikeId id) => id.Value;
    public static explicit operator RecipeLikeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
