using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CreateBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

public sealed partial class BrowserSessionServiceTests {
    [Fact]
    public async Task CreateCommand_ReplacesAProvedPreviousSessionAndForwardsTheAbsoluteCap() {
        var scenario = new Scenario();
        var previousId = RefreshTokenSessionId.New();
        string previousCredential = BrowserSessionCredential.Create(previousId.Value);
        var previous = UserRefreshTokenSession.Create(previousId, scenario.Session.UserId,
            SecurityTokenGenerator.HashForStorage(previousCredential), rememberMe: false, authProvider: "password",
            ipAddress: null, userAgent: null, Now.AddDays(-2), Now.AddDays(20));
        previous.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(previousCredential), 7, Now.AddDays(-1));
        scenario.Sessions.GetByIdAsync(previousId, Arg.Any<CancellationToken>()).Returns(previous);
        DateTime cap = Now.AddHours(1);

        Result<BrowserSessionModel> result = await new CreateBrowserSessionCommandHandler(scenario.Service)
            .Handle(new CreateBrowserSessionCommand("refresh-proof", previousCredential, cap), CancellationToken.None);

        BrowserSessionModel created = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(scenario.Session.Id.Value, created.SessionId),
            () => Assert.Equal(cap, created.ExpiresAtUtc),
            () => Assert.NotEqual(previousCredential, created.Credential, StringComparer.Ordinal),
            () => Assert.Equal(Now, previous.RevokedAtUtc),
            () => Assert.False(previous.IsActive));
        await scenario.Sessions.Received(1).UpdateAsync(previous, CancellationToken.None);
    }

    [Fact]
    public async Task Create_PrincipalFailureDoesNotConvertOrPersistTheRefreshSession() {
        var scenario = new Scenario();
        scenario.Users.GetAuthenticationPrincipalAsync(scenario.Session.UserId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(AuthenticationErrors.InvalidToken));

        Result<BrowserSessionModel> result = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);

        ResultAssert.Failure(result, AuthenticationErrors.InvalidToken.Code);
        Assert.Null(scenario.Session.BrowserSecurityVersion);
        await scenario.Sessions.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Authenticate_AbsoluteLifetimeCapRejectsAnOtherwiseUnexpiredRenewableSession() {
        var scenario = new Scenario();
        string credential = BrowserSessionCredential.Create(scenario.Session.Id.Value);
        scenario.Session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), 7, Now, allowRenewal: true);
        scenario.Lifetimes.GetAbsoluteExpiration(scenario.Session.CreatedAtUtc).Returns(Now);
        scenario.Sessions.ReadBrowserSessionProofAsync(scenario.Session.Id, Arg.Any<CancellationToken>())
            .Returns(new BrowserSessionProof(
                scenario.Session.Id, scenario.Session.UserId, scenario.Session.RefreshTokenHash, SecurityVersion: 7,
                scenario.Session.ExpiresAtUtc, RevokedAtUtc: null, Now, scenario.Session.CreatedAtUtc, SlidingExpirationEnabled: true));

        Result<BrowserSessionIdentityModel> result = await scenario.Service.AuthenticateAsync(credential, CancellationToken.None);

        ResultAssert.Failure(result, AuthenticationErrors.InvalidToken.Code);
    }

    [Fact]
    public async Task Restore_RememberConflictDoesNotReturnAPersistentSession() {
        var scenario = new Scenario();
        string credential = BrowserSessionCredential.Create(scenario.Session.Id.Value);
        scenario.Session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), 7, Now, allowRenewal: true);
        scenario.Sessions.TryRememberBrowserSessionAsync(Arg.Any<BrowserSessionProof>(),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(returnThis: false);

        Result<BrowserSessionModel> result = await scenario.Service.RestoreAsync(credential, rememberMe: true, CancellationToken.None);

        ResultAssert.Failure(result, AuthenticationErrors.InvalidToken.Code);
        Assert.False(scenario.Session.RememberMe);
    }
}
