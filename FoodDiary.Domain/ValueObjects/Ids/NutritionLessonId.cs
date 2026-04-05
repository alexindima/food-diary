using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct NutritionLessonId(Guid Value) : IEntityId<Guid> {
    public static NutritionLessonId New() => new(Guid.NewGuid());
    public static NutritionLessonId Empty => new(Guid.Empty);

    public static implicit operator Guid(NutritionLessonId id) => id.Value;
    public static explicit operator NutritionLessonId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
