using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationId(Guid Value) : IEntityId<Guid> {
    public static RecommendationId New() => new(Guid.NewGuid());
    public static RecommendationId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecommendationId id) => id.Value;
    public static explicit operator RecommendationId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
