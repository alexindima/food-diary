using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record DietologistInvitationAcceptedDomainEvent(
    DietologistInvitationId InvitationId,
    UserId ClientUserId,
    UserId DietologistUserId,
    DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? DomainTime.UtcNow;
}
