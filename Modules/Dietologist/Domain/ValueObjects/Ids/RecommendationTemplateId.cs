using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationTemplateId(Guid Value) : IEntityId<Guid> {
    public static RecommendationTemplateId New() => new(Guid.NewGuid());
    public static RecommendationTemplateId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecommendationTemplateId id) => id.Value;
    public static explicit operator RecommendationTemplateId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
