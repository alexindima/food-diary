using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class JwtTokenUseValidatorTests {
    [Fact]
    public void IsAccessToken_WithAccessTokenUse_ReturnsTrue() {
        ClaimsPrincipal principal = CreatePrincipal(JwtTokenUseClaimNames.Access);

        bool result = JwtTokenUseValidator.IsAccessToken(principal);

        Assert.True(result);
    }

    [Theory]
    [InlineData(JwtTokenUseClaimNames.Refresh)]
    [InlineData("other")]
    [InlineData(null)]
    public void IsAccessToken_WithoutAccessTokenUse_ReturnsFalse(string? tokenUse) {
        ClaimsPrincipal principal = CreatePrincipal(tokenUse);

        bool result = JwtTokenUseValidator.IsAccessToken(principal);

        Assert.False(result);
    }

    private static ClaimsPrincipal CreatePrincipal(string? tokenUse) {
        Claim[] claims = tokenUse is null ? [] : [new Claim(JwtTokenUseClaimNames.ClaimType, tokenUse)];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
