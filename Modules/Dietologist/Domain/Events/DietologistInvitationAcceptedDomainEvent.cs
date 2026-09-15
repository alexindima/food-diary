using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Events;

public sealed record DietologistInvitationAcceptedDomainEvent : IDomainEvent {
    public DietologistInvitationAcceptedDomainEvent(
        DietologistInvitationId invitationId,
        UserId clientUserId,
        UserId dietologistUserId,
        DateTime? occurredOnUtcOverride = null) {
        InvitationId = invitationId;
        ClientUserId = clientUserId;
        DietologistUserId = dietologistUserId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public DietologistInvitationId InvitationId { get; }
    public UserId ClientUserId { get; }
    public UserId DietologistUserId { get; }
    public DateTime OccurredOnUtc { get; }
}
