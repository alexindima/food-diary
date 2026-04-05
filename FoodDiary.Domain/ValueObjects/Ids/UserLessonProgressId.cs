using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct UserLessonProgressId(Guid Value) : IEntityId<Guid> {
    public static UserLessonProgressId New() => new(Guid.NewGuid());
    public static UserLessonProgressId Empty => new(Guid.Empty);

    public static implicit operator Guid(UserLessonProgressId id) => id.Value;
    public static explicit operator UserLessonProgressId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
