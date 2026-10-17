using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using System.Security.Claims;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class AccessTokenSecurityStateValidatorTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid")]
    public async Task IsCurrentAsync_WithMissingOrInvalidIdentity_RejectsBeforeLookup(string? id) {
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        ClaimsPrincipal? principal = id is null ? null : new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)]));
        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(principal, reader, Substitute.For<IUserAccessTokenSessionReader>(), CancellationToken.None));
        await reader.DidNotReceiveWithAnyArgs().IsCurrentAsync(default, default, default);
    }

    [Fact]
    public async Task IsCurrentAsync_WithMatchingSecurityVersion_AllowsToken() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        ClaimsPrincipal principal = CreatePrincipal(userId, securityVersion: "3");

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            principal,
            reader,
            CreateActiveSessionReader(),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCurrentAsync_WithLegacyToken_UsesVersionZero() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 0, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        ClaimsPrincipal principal = CreatePrincipal(userId, securityVersion: null);

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            principal,
            reader,
            CreateActiveSessionReader(),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCurrentAsync_WithStaleSecurityVersion_RejectsToken() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 2, Arg.Any<CancellationToken>()).Returns(returnThis: false);
        ClaimsPrincipal principal = CreatePrincipal(userId, securityVersion: "2");

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            principal,
            reader,
            CreateActiveSessionReader(),
            CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    public async Task IsCurrentAsync_WithInvalidSecurityVersion_RejectsBeforeLookup(string securityVersion) {
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        ClaimsPrincipal principal = CreatePrincipal(Guid.NewGuid(), securityVersion);

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            principal,
            reader,
            CreateActiveSessionReader(),
            CancellationToken.None);

        Assert.False(result);
        await reader.DidNotReceiveWithAnyArgs().IsCurrentAsync(default, default, default);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string? securityVersion) {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtSecurityClaimNames.RefreshSessionId, Guid.NewGuid().ToString()),
        };
        if (securityVersion is not null) {
            claims.Add(new Claim(JwtSecurityClaimNames.SecurityVersion, securityVersion));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private static IUserAccessTokenSessionReader CreateActiveSessionReader() {
        IUserAccessTokenSessionReader reader = Substitute.For<IUserAccessTokenSessionReader>();
        reader.IsActiveAsync(default, default, default).ReturnsForAnyArgs(returnThis: true);
        return reader;
    }

    [Fact]
    public async Task IsCurrentAsync_WithRevokedSession_RejectsToken() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        IUserAccessTokenSessionReader sessions = Substitute.For<IUserAccessTokenSessionReader>();
        ClaimsPrincipal principal = CreatePrincipal(userId, "3");

        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(principal, reader, sessions, CancellationToken.None));
        await sessions.Received(1).IsActiveAsync(
            userId,
            Guid.Parse(principal.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId)!), CancellationToken.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task IsCurrentAsync_WithoutValidSession_RejectsToken(string? sessionId) {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()), new(JwtSecurityClaimNames.SecurityVersion, "3") };
        if (sessionId is not null) {
            claims.Add(new Claim(JwtSecurityClaimNames.RefreshSessionId, sessionId));
        }
        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(
            new ClaimsPrincipal(new ClaimsIdentity(claims)), reader, CreateActiveSessionReader(), CancellationToken.None));
    }

    [Fact]
    public async Task IsCurrentAsync_WithLegacyImpersonation_RejectsUnboundToken() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(JwtSecurityClaimNames.SecurityVersion, "3"),
            new Claim(FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions.JwtImpersonationClaimNames.IsImpersonation, "true"),
            new Claim(FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions.JwtImpersonationClaimNames.ActorUserId, Guid.NewGuid().ToString()),
        ]));
        IUserAccessTokenSessionReader sessions = Substitute.For<IUserAccessTokenSessionReader>();
        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(principal, reader, sessions, CancellationToken.None));
        await sessions.DidNotReceiveWithAnyArgs().IsActiveAsync(default, default, default);
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public async Task IsCurrentAsync_WithImpersonation_RequiresCurrentTargetAdministratorAndOriginatingSession(
        bool targetCurrent, bool actorCurrent, bool sessionActive, bool expected) {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(targetId, 3, cancellation.Token).Returns(targetCurrent);
        reader.IsCurrentAdministratorAsync(actorId, 5, cancellation.Token).Returns(actorCurrent);
        IUserAccessTokenSessionReader sessions = Substitute.For<IUserAccessTokenSessionReader>();
        sessions.IsActiveAsync(actorId, sessionId, cancellation.Token).Returns(sessionActive);

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            CreateBoundPrincipal(targetId, actorId, sessionId), reader, sessions, cancellation.Token);

        Assert.Equal(expected, result);
        if (targetCurrent) {
            await reader.Received(1).IsCurrentAdministratorAsync(actorId, 5, cancellation.Token);
        } else {
            await reader.DidNotReceiveWithAnyArgs().IsCurrentAdministratorAsync(default, default, default);
        }
        if (targetCurrent && actorCurrent) {
            await sessions.Received(1).IsActiveAsync(actorId, sessionId, cancellation.Token);
        } else {
            await sessions.DidNotReceiveWithAnyArgs().IsActiveAsync(default, default, default);
        }
    }

    [Theory]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, null)]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, "")]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, "-1")]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, "+5")]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, " 5")]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, "not-a-number")]
    [InlineData(JwtImpersonationClaimNames.ActorSecurityVersion, "9223372036854775808")]
    [InlineData(JwtImpersonationClaimNames.ActorSessionId, null)]
    [InlineData(JwtImpersonationClaimNames.ActorSessionId, "invalid")]
    [InlineData(JwtImpersonationClaimNames.ActorSessionId, "00000000-0000-0000-0000-000000000000")]
    [InlineData(JwtImpersonationClaimNames.ActorUserId, null)]
    [InlineData(JwtImpersonationClaimNames.ActorUserId, "invalid")]
    [InlineData(JwtImpersonationClaimNames.ActorUserId, "00000000-0000-0000-0000-000000000000")]
    [InlineData(JwtImpersonationClaimNames.IsImpersonation, "TRUE")]
    [InlineData(JwtImpersonationClaimNames.IsImpersonation, "false")]
    public async Task IsCurrentAsync_WithMalformedImpersonationBinding_RejectsBeforeActorLookups(string type, string? value) {
        var targetId = Guid.NewGuid();
        ClaimsPrincipal principal = CreateBoundPrincipal(targetId, Guid.NewGuid(), Guid.NewGuid());
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.RemoveClaim(identity.FindFirst(type));
        if (value is not null) {
            identity.AddClaim(new Claim(type, value));
        }
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(targetId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        IUserAccessTokenSessionReader sessions = CreateActiveSessionReader();

        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(principal, reader, sessions, CancellationToken.None));
        await reader.DidNotReceiveWithAnyArgs().IsCurrentAdministratorAsync(default, default, default);
        await sessions.DidNotReceiveWithAnyArgs().IsActiveAsync(default, default, default);
    }

    [Fact]
    public async Task IsCurrentAsync_WithTargetAsActor_RejectsToken() {
        var targetId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(targetId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);

        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(
            CreateBoundPrincipal(targetId, targetId, Guid.NewGuid()), reader, CreateActiveSessionReader(), CancellationToken.None));
    }

    [Fact]
    public async Task IsCurrentAsync_WithAnotherActorOrSessionActive_RejectsUnrelatedBinding() {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(targetId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        reader.IsCurrentAdministratorAsync(actorId, 5, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        IUserAccessTokenSessionReader sessions = Substitute.For<IUserAccessTokenSessionReader>();
        sessions.IsActiveAsync(Guid.NewGuid(), sessionId, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        sessions.IsActiveAsync(actorId, Guid.NewGuid(), Arg.Any<CancellationToken>()).Returns(returnThis: true);

        Assert.False(await AccessTokenSecurityStateValidator.IsCurrentAsync(
            CreateBoundPrincipal(targetId, actorId, sessionId), reader, sessions, CancellationToken.None));
    }

    private static ClaimsPrincipal CreateBoundPrincipal(Guid targetId, Guid actorId, Guid sessionId) =>
        new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, targetId.ToString()),
            new Claim(JwtSecurityClaimNames.SecurityVersion, "3"),
            new Claim(JwtImpersonationClaimNames.IsImpersonation, "true"),
            new Claim(JwtImpersonationClaimNames.ActorUserId, actorId.ToString()),
            new Claim(JwtImpersonationClaimNames.ActorSecurityVersion, "5"),
            new Claim(JwtImpersonationClaimNames.ActorSessionId, sessionId.ToString()),
        ]));
}
