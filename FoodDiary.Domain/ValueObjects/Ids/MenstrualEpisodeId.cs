using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MenstrualEpisodeId(Guid Value) : IEntityId<Guid> {
    public static MenstrualEpisodeId New() => new(Guid.NewGuid());
    public static MenstrualEpisodeId Empty => new(Guid.Empty);

    public static implicit operator Guid(MenstrualEpisodeId id) => id.Value;
    public static explicit operator MenstrualEpisodeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
