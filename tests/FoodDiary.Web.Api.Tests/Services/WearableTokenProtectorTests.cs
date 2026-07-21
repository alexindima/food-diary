using FoodDiary.Web.Api.Services;
using Microsoft.AspNetCore.DataProtection;

namespace FoodDiary.Web.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WearableTokenProtectorTests {
    [Fact]
    public void ProtectAndUnprotect_RoundTripsPlainToken() {
        WearableTokenProtector protector = CreateProtector();

        string protectedToken = protector.Protect("access-token");
        string unprotectedToken = protector.Unprotect(protectedToken);

        Assert.StartsWith("fdp1:", protectedToken, StringComparison.Ordinal);
        Assert.Equal("access-token", unprotectedToken);
    }

    [Fact]
    public void Protect_WhenAlreadyProtected_ReturnsTokenUnchanged() {
        WearableTokenProtector protector = CreateProtector();
        string protectedToken = protector.Protect("access-token");

        string protectedAgain = protector.Protect(protectedToken);

        Assert.Equal(protectedToken, protectedAgain);
    }

    [Fact]
    public void Unprotect_WhenTokenIsPlain_ReturnsTokenUnchanged() {
        WearableTokenProtector protector = CreateProtector();

        string token = protector.Unprotect("access-token");

        Assert.Equal("access-token", token);
    }

    private static WearableTokenProtector CreateProtector() {
        IDataProtectionProvider provider = new EphemeralDataProtectionProvider();
        return new WearableTokenProtector(provider);
    }
}
