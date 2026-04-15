using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record DietologistInvitationDeclinedDomainEvent(
    DietologistInvitationId InvitationId,
    UserId ClientUserId,
    string DietologistEmail,
    DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? DomainTime.UtcNow;
}
