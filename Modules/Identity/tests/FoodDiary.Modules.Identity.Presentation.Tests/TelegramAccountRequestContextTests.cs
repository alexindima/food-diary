using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Security;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramAccountRequestContextTests {
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("7", 7L)]
    [InlineData("-1", null)]
    [InlineData("+1", null)]
    [InlineData("1,000", null)]
    [InlineData("invalid", null)]
    public void ReadSecurityVersion_RequiresAnUnambiguousNonnegativeClaim(string value, long? expected) {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtSecurityClaimNames.SecurityVersion, value)])) };
        Assert.Equal(expected, TelegramAccountRequestContext.ReadSecurityVersion(context));
    }

    [Fact]
    public void ReadSecurityVersion_RejectsMissingAndDuplicateClaims() {
        var context = new DefaultHttpContext();
        Assert.Null(TelegramAccountRequestContext.ReadSecurityVersion(context));
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtSecurityClaimNames.SecurityVersion, "0"), new Claim(JwtSecurityClaimNames.SecurityVersion, "0")]));
        Assert.Null(TelegramAccountRequestContext.ReadSecurityVersion(context));
    }
}
