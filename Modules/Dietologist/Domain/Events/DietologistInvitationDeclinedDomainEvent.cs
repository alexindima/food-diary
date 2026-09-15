using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Events;

public sealed record DietologistInvitationDeclinedDomainEvent : IDomainEvent {
    public DietologistInvitationDeclinedDomainEvent(
        DietologistInvitationId invitationId,
        UserId clientUserId,
        string dietologistEmail,
        DateTime? occurredOnUtcOverride = null) {
        InvitationId = invitationId;
        ClientUserId = clientUserId;
        DietologistEmail = dietologistEmail;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public DietologistInvitationId InvitationId { get; }
    public UserId ClientUserId { get; }
    public string DietologistEmail { get; }
    public DateTime OccurredOnUtc { get; }
}
