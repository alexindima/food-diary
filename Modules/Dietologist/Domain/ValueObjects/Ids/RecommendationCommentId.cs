using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationCommentId(Guid Value) : IEntityId<Guid> {
    public static RecommendationCommentId New() => new(Guid.NewGuid());
    public static RecommendationCommentId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecommendationCommentId id) => id.Value;
    public static explicit operator RecommendationCommentId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
