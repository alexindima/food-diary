using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class DietologistInvitation : AggregateRoot<DietologistInvitationId> {
    public UserId ClientUserId { get; private set; }
    public UserId? DietologistUserId { get; private set; }
    public string DietologistEmail { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DietologistInvitationStatus Status { get; private set; }
    public bool ShareProfile { get; private set; }
    public bool ShareMeals { get; private set; }
    public bool ShareStatistics { get; private set; }
    public bool ShareWeight { get; private set; }
    public bool ShareWaist { get; private set; }
    public bool ShareGoals { get; private set; }
    public bool ShareHydration { get; private set; }
    public bool ShareFasting { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public User ClientUser { get; private set; } = null!;
    public User? DietologistUser { get; private set; }

    private DietologistInvitation() {
    }

    public static DietologistInvitation Create(
        UserId clientUserId,
        string dietologistEmail,
        string tokenHash,
        DateTime expiresAtUtc,
        DietologistPermissions permissions) {
        EnsureUserId(clientUserId);

        if (string.IsNullOrWhiteSpace(dietologistEmail)) {
            throw new ArgumentException("Dietologist email is required.", nameof(dietologistEmail));
        }

        if (string.IsNullOrWhiteSpace(tokenHash)) {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        var invitation = new DietologistInvitation {
            Id = DietologistInvitationId.New(),
            ClientUserId = clientUserId,
            DietologistEmail = dietologistEmail.Trim().ToLowerInvariant(),
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            Status = DietologistInvitationStatus.Pending,
        };
        invitation.ApplyPermissions(permissions);
        invitation.SetCreated();
        return invitation;
    }

    public void Accept(UserId dietologistUserId) {
        EnsureUserId(dietologistUserId);
        EnsurePending();

        DietologistUserId = dietologistUserId;
        Status = DietologistInvitationStatus.Accepted;
        AcceptedAtUtc = DomainTime.UtcNow;
        SetModified();
        RaiseDomainEvent(new DietologistInvitationAcceptedDomainEvent(Id, ClientUserId, dietologistUserId));
    }

    public void Decline() {
        EnsurePending();

        Status = DietologistInvitationStatus.Declined;
        SetModified();
    }

    public void Revoke() {
        if (Status is not (DietologistInvitationStatus.Pending or DietologistInvitationStatus.Accepted)) {
            throw new InvalidOperationException($"Cannot revoke invitation in status {Status}.");
        }

        Status = DietologistInvitationStatus.Revoked;
        RevokedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void UpdatePermissions(DietologistPermissions permissions) {
        ApplyPermissions(permissions);
        SetModified();
    }

    public bool IsExpired() =>
        Status == DietologistInvitationStatus.Pending && ExpiresAtUtc < DomainTime.UtcNow;

    public DietologistPermissions GetPermissions() => new(
        ShareMeals, ShareStatistics, ShareWeight, ShareWaist, ShareGoals, ShareHydration, ShareProfile, ShareFasting);

    private void ApplyPermissions(DietologistPermissions permissions) {
        ShareProfile = permissions.ShareProfile;
        ShareMeals = permissions.ShareMeals;
        ShareStatistics = permissions.ShareStatistics;
        ShareWeight = permissions.ShareWeight;
        ShareWaist = permissions.ShareWaist;
        ShareGoals = permissions.ShareGoals;
        ShareHydration = permissions.ShareHydration;
        ShareFasting = permissions.ShareFasting;
    }

    private void EnsurePending() {
        if (Status != DietologistInvitationStatus.Pending) {
            throw new InvalidOperationException($"Invitation is not pending (current status: {Status}).");
        }

        if (IsExpired()) {
            throw new InvalidOperationException("Invitation has expired.");
        }
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
