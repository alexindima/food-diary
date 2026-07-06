using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public class DietologistAccessPolicyTests {
    [Fact]
    public async Task EnsureCanAccessClientAsync_WithNoActiveInvitation_ReturnsFailure() {
        IDietologistInvitationRepository repo = CreateInvitationRepository(invitation: null);

        Result<DietologistPermissionsModel> result = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            repo, UserId.New(), UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccessDenied", result.Error.Code, StringComparison.Ordinal);
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

        IDietologistInvitationRepository repo = CreateInvitationRepository(invitation);

        Result<DietologistPermissionsModel> result = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            repo, dietologistId, clientId, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.ShareMeals);
        Assert.False(result.Value.ShareStatistics);
        Assert.True(result.Value.ShareWeight);
        Assert.False(result.Value.ShareWaist);
        Assert.True(result.Value.ShareProfile);
        Assert.True(result.Value.ShareFasting);
    }

    [Fact]
    public void EnsurePermission_WhenPermissionGranted_ReturnsNull() {
        var perms = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true);

        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Profile"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Meals"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Statistics"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Weight"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Waist"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Goals"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Hydration"));
        Assert.Null(DietologistAccessPolicy.EnsurePermission(perms, "Fasting"));
    }

    [Fact]
    public void EnsurePermission_WhenMealsDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(ShareMeals: false, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true);

        Error? error = DietologistAccessPolicy.EnsurePermission(perms, "Meals");

        Assert.NotNull(error);
        Assert.Contains("PermissionDenied", error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsurePermission_WhenHydrationDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: false, ShareProfile: true, ShareFasting: true);

        Error? error = DietologistAccessPolicy.EnsurePermission(perms, "Hydration");

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("Statistics")]
    [InlineData("Weight")]
    [InlineData("Waist")]
    [InlineData("Goals")]
    public void EnsurePermission_WhenSpecificPermissionDenied_ReturnsError(string category) {
        DietologistPermissionsModel perms = category switch {
            "Statistics" => new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: false, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true),
            "Weight" => new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: false, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true),
            "Waist" => new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: false, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true),
            "Goals" => new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: false, ShareHydration: true, ShareProfile: true, ShareFasting: true),
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };

        Error? error = DietologistAccessPolicy.EnsurePermission(perms, category);

        Assert.NotNull(error);
        Assert.Contains("PermissionDenied", error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsurePermission_WhenProfileDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: false, ShareFasting: true);

        Error? error = DietologistAccessPolicy.EnsurePermission(perms, "Profile");

        Assert.NotNull(error);
    }

    [Fact]
    public void EnsurePermission_WhenFastingDenied_ReturnsError() {
        var perms = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: false);

        Error? error = DietologistAccessPolicy.EnsurePermission(perms, "Fasting");

        Assert.NotNull(error);
    }

    [Fact]
    public void EnsurePermission_WithUnknownCategory_ReturnsError() {
        var perms = new DietologistPermissionsModel(ShareMeals: false, ShareStatistics: false, ShareWeight: false, ShareWaist: false, ShareGoals: false, ShareHydration: false, ShareProfile: false, ShareFasting: false);

        Assert.NotNull(DietologistAccessPolicy.EnsurePermission(perms, "Unknown"));
    }

    private static IDietologistInvitationRepository CreateInvitationRepository(DietologistInvitation? invitation) {
        IDietologistInvitationRepository repository = Substitute.For<IDietologistInvitationRepository>();
        ((IDietologistInvitationReadRepository)repository)
            .GetActiveByClientAndDietologistAsync(Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(invitation));
        return repository;
    }
}
