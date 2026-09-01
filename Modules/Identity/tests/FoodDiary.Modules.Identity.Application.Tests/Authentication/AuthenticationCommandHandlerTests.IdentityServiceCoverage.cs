using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    private static readonly DateTime IdentityCoverageNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task IdentityService_InactiveAccounts_ReturnInvalidCredentials() {
        var passwordUser = User.Create("inactive-password@example.com", "secret");
        passwordUser.Deactivate(IdentityCoverageNow);
        var googleUser = User.Create("inactive-google@example.com", "secret", hasPassword: false);
        googleUser.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        googleUser.Deactivate(IdentityCoverageNow);
        var principalUser = User.Create("inactive-principal@example.com", "secret");
        principalUser.Deactivate(IdentityCoverageNow);

        Result<UserAuthenticationPrincipalModel> password = await CreateUserAuthenticationIdentityService(
                new StubUserRepository(passwordUser))
            .AuthenticatePasswordAsync(passwordUser.Email, "secret", IdentityCoverageNow, CancellationToken.None);
        Result<UserAuthenticationPrincipalModel> google = await CreateUserAuthenticationIdentityService(
                new StubUserRepository(googleUser))
            .AuthenticateGoogleAsync(
                new UserGoogleAuthenticationModel(
                    GoogleIssuer, GoogleSubject, googleUser.Email, FirstName: null, LastName: null, Locale: null),
                IdentityCoverageNow,
                CancellationToken.None);
        Result<UserAuthenticationPrincipalModel> principal = await CreateUserAuthenticationIdentityService(
                new StubUserRepository(principalUser))
            .GetAuthenticationPrincipalAsync(principalUser.Id, IdentityCoverageNow, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal("Authentication.InvalidCredentials", ResultAssert.Failure(password).Code),
            () => Assert.Equal("Authentication.InvalidCredentials", ResultAssert.Failure(google).Code),
            () => Assert.Equal("Authentication.InvalidCredentials", ResultAssert.Failure(principal).Code));
    }

    [Fact]
    public async Task IdentityService_GetAuthenticationPrincipalAsync_ForDeletedAccount_ReturnsAccountDeleted() {
        var user = User.Create("deleted-principal@example.com", "secret");
        user.DeleteAccount(IdentityCoverageNow);

        Result<UserAuthenticationPrincipalModel> result = await CreateUserAuthenticationIdentityService(
                new StubUserRepository(user))
            .GetAuthenticationPrincipalAsync(user.Id, IdentityCoverageNow, CancellationToken.None);

        Assert.Equal("Authentication.AccountDeleted", ResultAssert.Failure(result).Code);
    }

    [Fact]
    public void ToAuthenticationPrincipal_ActiveTrialAddsPremiumRoleAndCapsToken() {
        var trialUser = User.Create("trial-principal@example.com", "secret");
        trialUser.StartPremiumTrial(IdentityCoverageNow.AddDays(-1), TimeSpan.FromDays(7));
        var paidUser = User.Create("paid-principal@example.com", "secret");
        paidUser.ReplaceRoles([Role.Create(RoleNames.Premium)]);
        paidUser.StartPremiumTrial(IdentityCoverageNow.AddDays(-1), TimeSpan.FromDays(7));

        UserAuthenticationPrincipalModel trial = UserAuthenticationIdentityService.ToAuthenticationPrincipal(
            trialUser, IdentityCoverageNow);
        UserAuthenticationPrincipalModel paid = UserAuthenticationIdentityService.ToAuthenticationPrincipal(
            paidUser, IdentityCoverageNow);

        Assert.Multiple(
            () => Assert.Contains(RoleNames.Premium, trial.Roles, StringComparer.Ordinal),
            () => Assert.Equal(trialUser.PremiumTrialEndsAtUtc, trial.AccessTokenCapUtc),
            () => Assert.Contains(RoleNames.Premium, paid.Roles, StringComparer.Ordinal),
            () => Assert.Null(paid.AccessTokenCapUtc));
    }
}
