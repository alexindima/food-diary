using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FastingTelemetryEventId(Guid Value) : IEntityId<Guid> {
    public static FastingTelemetryEventId New() => new(Guid.NewGuid());
    public static FastingTelemetryEventId Empty => new(Guid.Empty);

    public static implicit operator Guid(FastingTelemetryEventId id) => id.Value;
    public static explicit operator FastingTelemetryEventId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
