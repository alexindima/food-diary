using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class AccessTokenSecurityStateValidatorTests {
    [Fact]
    public async Task IsCurrentAsync_WithMatchingSecurityVersion_AllowsToken() {
        var userId = Guid.NewGuid();
        IUserAccessTokenSecurityReader reader = Substitute.For<IUserAccessTokenSecurityReader>();
        reader.IsCurrentAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        ClaimsPrincipal principal = CreatePrincipal(userId, securityVersion: "3");

        bool result = await AccessTokenSecurityStateValidator.IsCurrentAsync(
            principal,
            reader,
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
            CancellationToken.None);

        Assert.False(result);
        await reader.DidNotReceiveWithAnyArgs().IsCurrentAsync(default, default, default);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string? securityVersion) {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
        };
        if (securityVersion is not null) {
            claims.Add(new Claim(JwtSecurityClaimNames.SecurityVersion, securityVersion));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
