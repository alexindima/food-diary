using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record DietologistInvitationDeclinedDomainEvent : IDomainEvent {
    public DietologistInvitationDeclinedDomainEvent(
        DietologistInvitationId invitationId,
        UserId clientUserId,
        string dietologistEmail,
        DateTime? occurredOnUtcOverride = null) {
        InvitationId = invitationId;
        ClientUserId = clientUserId;
        DietologistEmail = dietologistEmail;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public DietologistInvitationId InvitationId { get; }
    public UserId ClientUserId { get; }
    public string DietologistEmail { get; }
    public DateTime OccurredOnUtc { get; }
}
