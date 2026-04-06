using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ContentReportId(Guid Value) : IEntityId<Guid> {
    public static ContentReportId New() => new(Guid.NewGuid());
    public static ContentReportId Empty => new(Guid.Empty);

    public static implicit operator Guid(ContentReportId id) => id.Value;
    public static explicit operator ContentReportId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
