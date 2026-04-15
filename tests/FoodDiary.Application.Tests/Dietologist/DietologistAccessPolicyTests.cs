using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public class DietologistAccessPolicyTests {
    [Fact]
    public async Task EnsureCanAccessClientAsync_WithNoActiveInvitation_ReturnsFailure() {
        var repo = new StubInvitationRepository(null);

        var result = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            repo, UserId.New(), UserId.New(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task EnsureCanAccessClientAsync_WithActiveInvitation_ReturnsPermissions() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId, "diet@example.com", "hash", DateTime.UtcNow.AddDays(7),
            new DietologistPermissions(ShareMeals: true, ShareStatistics: false,
                ShareWeight: true, ShareWaist: false, ShareGoals: true, ShareHydration: false));
        invitation.Accept(dietologistId);

        var repo = new StubInvitationRepository(invitation);

        var result = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            repo, dietologistId, clientId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.ShareMeals);
        Assert.False(result.Value.ShareStatistics);
        Assert.True(result.Value.ShareWeight);
        Assert.False(result.Value.ShareWaist);
        Assert.True(result.Value.ShareProfile);
        Assert.True(result.Value.ShareFasting);
    }

    [Fact]
    public void EnsurePermission_WhenPermissionGranted_ReturnsNull() {
        var perms = new DietologistPermissionsModel(true, true, true, true, true, true, true, true);

        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Profile"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Meals"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Statistics"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Weight"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Fasting"));
    }

    [Fact]
    public void EnsurePermission_WhenMealsDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(false, true, true, true, true, true, true, true);

        var error = DietologistAccessPolicy.EnsurePermission(perms, "Meals");

        Assert.NotNull(error);
        Assert.Contains("PermissionDenied", error.Code);
    }

    [Fact]
    public void EnsurePermission_WhenHydrationDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(true, true, true, true, true, false, true, true);

        var error = DietologistAccessPolicy.EnsurePermission(perms, "Hydration");

        Assert.NotNull(error);
    }

    [Fact]
    public void EnsurePermission_WhenProfileDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(true, true, true, true, true, true, false, true);

        var error = DietologistAccessPolicy.EnsurePermission(perms, "Profile");

        Assert.NotNull(error);
    }

    [Fact]
    public void EnsurePermission_WhenFastingDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(true, true, true, true, true, true, true, false);

        var error = DietologistAccessPolicy.EnsurePermission(perms, "Fasting");

        Assert.NotNull(error);
    }

    [Fact]
    public void EnsurePermission_WithUnknownCategory_ReturnsNull() {
        var perms = new DietologistPermissionsModel(false, false, false, false, false, false, false, false);

        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Unknown"));
    }

    private sealed class StubInvitationRepository(DietologistInvitation? invitation) : IDietologistInvitationRepository {
        public Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(invitation);

        public Task<DietologistInvitation?> GetByIdAsync(
            DietologistInvitationId id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DietologistInvitation?> GetByClientAndStatusAsync(
            UserId clientUserId, DietologistInvitationStatus status, bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DietologistInvitation?> GetActiveByClientAsync(
            UserId clientUserId, bool asTracking = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DietologistInvitation?> GetPendingByClientAndEmailAsync(
            UserId clientUserId, string email, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
            UserId dietologistUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasActiveRelationshipAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DietologistInvitation> AddAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
