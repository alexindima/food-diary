using System.Security.Claims;
using FoodDiary.Presentation.Api.Extensions;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class UserExtensionsTests {
    [Fact]
    public void GetUserGuid_WithNameIdentifierClaim_ReturnsGuid() {
        var expected = Guid.NewGuid();
        var user = CreateUser(new Claim(ClaimTypes.NameIdentifier, expected.ToString()));

        var result = user.GetUserGuid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetUserGuid_WithNameIdClaim_ReturnsGuid() {
        var expected = Guid.NewGuid();
        var user = CreateUser(new Claim("nameid", expected.ToString()));

        var result = user.GetUserGuid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetUserGuid_WithSubClaim_ReturnsGuid() {
        var expected = Guid.NewGuid();
        var user = CreateUser(new Claim("sub", expected.ToString()));

        var result = user.GetUserGuid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetUserGuid_WithNoSupportedClaim_ReturnsNull() {
        var user = CreateUser(new Claim(ClaimTypes.Email, "user@example.com"));

        var result = user.GetUserGuid();

        Assert.Null(result);
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));
}
