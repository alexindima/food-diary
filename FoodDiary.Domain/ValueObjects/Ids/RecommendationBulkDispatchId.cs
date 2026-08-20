using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationBulkDispatchId(Guid Value) : IEntityId<Guid> {
    public static RecommendationBulkDispatchId New() => new(Guid.NewGuid());
    public static RecommendationBulkDispatchId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecommendationBulkDispatchId id) => id.Value;
    public static explicit operator RecommendationBulkDispatchId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
