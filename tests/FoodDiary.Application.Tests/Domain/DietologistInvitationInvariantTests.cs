using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class DietologistInvitationInvariantTests {
    [Fact]
    public void Create_WithEmptyClientUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            DietologistInvitation.Create(
                UserId.Empty,
                "diet@example.com",
                "hash",
                DateTime.UtcNow.AddDays(7),
                DietologistPermissions.AllEnabled));
    }

    [Fact]
    public void Create_WithBlankEmail_Throws() {
        Assert.Throws<ArgumentException>(() =>
            DietologistInvitation.Create(
                UserId.New(),
                "   ",
                "hash",
                DateTime.UtcNow.AddDays(7),
                DietologistPermissions.AllEnabled));
    }

    [Fact]
    public void Create_WithBlankTokenHash_Throws() {
        Assert.Throws<ArgumentException>(() =>
            DietologistInvitation.Create(
                UserId.New(),
                "diet@example.com",
                "  ",
                DateTime.UtcNow.AddDays(7),
                DietologistPermissions.AllEnabled));
    }

    [Fact]
    public void Create_NormalizesEmailToLowerTrimmed() {
        var invitation = DietologistInvitation.Create(
            UserId.New(),
            "  DIET@Example.COM  ",
            "hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled);

        Assert.Equal("diet@example.com", invitation.DietologistEmail);
    }

    [Fact]
    public void Create_SetsStatusToPending() {
        var invitation = DietologistInvitation.Create(
            UserId.New(),
            "diet@example.com",
            "hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled);

        Assert.Equal(DietologistInvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void Create_AppliesPermissions() {
        var perms = new DietologistPermissions(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: true,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false);

        var invitation = DietologistInvitation.Create(
            UserId.New(),
            "diet@example.com",
            "hash",
            DateTime.UtcNow.AddDays(7),
            perms);

        Assert.True(invitation.ShareMeals);
        Assert.False(invitation.ShareStatistics);
        Assert.True(invitation.ShareWeight);
        Assert.False(invitation.ShareWaist);
        Assert.True(invitation.ShareGoals);
        Assert.False(invitation.ShareHydration);
    }

    [Fact]
    public void GetPermissions_ReturnsMatchingRecord() {
        var perms = new DietologistPermissions(
            ShareMeals: false, ShareStatistics: true, ShareWeight: false,
            ShareWaist: true, ShareGoals: false, ShareHydration: true);
        var invitation = DietologistInvitation.Create(
            UserId.New(), "d@e.com", "hash", DateTime.UtcNow.AddDays(7), perms);

        Assert.Equal(perms, invitation.GetPermissions());
    }

    [Fact]
    public void Accept_WithEmptyDietologistUserId_Throws() {
        var invitation = CreatePendingInvitation();

        Assert.Throws<ArgumentException>(() => invitation.Accept(UserId.Empty));
    }

    [Fact]
    public void Accept_SetsStatusToAccepted_AndRaisesEvent() {
        var invitation = CreatePendingInvitation();
        var dietologistId = UserId.New();

        invitation.Accept(dietologistId);

        Assert.Equal(DietologistInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(dietologistId, invitation.DietologistUserId);
        Assert.NotNull(invitation.AcceptedAtUtc);
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_Throws() {
        var invitation = CreatePendingInvitation();
        invitation.Accept(UserId.New());

        Assert.Throws<InvalidOperationException>(() => invitation.Accept(UserId.New()));
    }

    [Fact]
    public void Decline_SetsStatusToDeclined() {
        var invitation = CreatePendingInvitation();

        invitation.Decline();

        Assert.Equal(DietologistInvitationStatus.Declined, invitation.Status);
    }

    [Fact]
    public void Decline_WhenNotPending_Throws() {
        var invitation = CreatePendingInvitation();
        invitation.Decline();

        Assert.Throws<InvalidOperationException>(() => invitation.Decline());
    }

    [Fact]
    public void Revoke_WhenPending_SetsStatusToRevoked() {
        var invitation = CreatePendingInvitation();

        invitation.Revoke();

        Assert.Equal(DietologistInvitationStatus.Revoked, invitation.Status);
        Assert.NotNull(invitation.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_WhenAccepted_SetsStatusToRevoked() {
        var invitation = CreatePendingInvitation();
        invitation.Accept(UserId.New());

        invitation.Revoke();

        Assert.Equal(DietologistInvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public void Revoke_WhenDeclined_Throws() {
        var invitation = CreatePendingInvitation();
        invitation.Decline();

        Assert.Throws<InvalidOperationException>(() => invitation.Revoke());
    }

    [Fact]
    public void UpdatePermissions_ChangesPermissions() {
        var invitation = CreatePendingInvitation();
        var newPerms = new DietologistPermissions(
            ShareMeals: false, ShareStatistics: false, ShareWeight: false,
            ShareWaist: false, ShareGoals: false, ShareHydration: false);

        invitation.UpdatePermissions(newPerms);

        Assert.Equal(newPerms, invitation.GetPermissions());
    }

    [Fact]
    public void IsExpired_WhenPendingAndPastExpiry_ReturnsTrue() {
        var invitation = DietologistInvitation.Create(
            UserId.New(),
            "d@e.com",
            "hash",
            DateTime.UtcNow.AddMinutes(-1),
            DietologistPermissions.AllEnabled);

        Assert.True(invitation.IsExpired());
    }

    [Fact]
    public void IsExpired_WhenPendingAndBeforeExpiry_ReturnsFalse() {
        var invitation = CreatePendingInvitation();

        Assert.False(invitation.IsExpired());
    }

    [Fact]
    public void Accept_WhenExpired_Throws() {
        var invitation = DietologistInvitation.Create(
            UserId.New(),
            "d@e.com",
            "hash",
            DateTime.UtcNow.AddMinutes(-1),
            DietologistPermissions.AllEnabled);

        Assert.Throws<InvalidOperationException>(() => invitation.Accept(UserId.New()));
    }

    private static DietologistInvitation CreatePendingInvitation() {
        return DietologistInvitation.Create(
            UserId.New(),
            "diet@example.com",
            "token-hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled);
    }
}
