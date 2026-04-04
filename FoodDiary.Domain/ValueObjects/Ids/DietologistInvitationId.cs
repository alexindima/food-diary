using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct DietologistInvitationId(Guid Value) : IEntityId<Guid> {
    public static DietologistInvitationId New() => new(Guid.NewGuid());
    public static DietologistInvitationId Empty => new(Guid.Empty);

    public static implicit operator Guid(DietologistInvitationId id) => id.Value;
    public static explicit operator DietologistInvitationId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
