using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record DietologistInvitationAcceptedDomainEvent : IDomainEvent {
    public DietologistInvitationAcceptedDomainEvent(
        DietologistInvitationId invitationId,
        UserId clientUserId,
        UserId dietologistUserId,
        DateTime? occurredOnUtcOverride = null) {
        InvitationId = invitationId;
        ClientUserId = clientUserId;
        DietologistUserId = dietologistUserId;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public DietologistInvitationId InvitationId { get; }
    public UserId ClientUserId { get; }
    public UserId DietologistUserId { get; }
    public DateTime OccurredOnUtc { get; }
}
