using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;

public readonly record struct RefreshTokenSessionId(Guid Value) : IEntityId<Guid> {
    public static RefreshTokenSessionId New() => new(Guid.NewGuid());
    public static RefreshTokenSessionId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
