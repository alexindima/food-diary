using FoodDiary.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Web.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WearableTokenProtectorTests {
    [Fact]
    public void ProtectAndUnprotect_RoundTripsPlainToken() {
        WearableTokenProtector protector = CreateProtector();

        ProtectedWearableToken protectedToken = protector.Protect("access-token");
        string unprotectedToken = protector.Unprotect(protectedToken);

        Assert.StartsWith("fdp1:", protectedToken.Value, StringComparison.Ordinal);
        Assert.Equal("access-token", unprotectedToken);
    }

    [Fact]
    public void Protect_WhenRawTokenStartsWithProtectedPrefix_StillProtectsIt() {
        WearableTokenProtector protector = CreateProtector();

        ProtectedWearableToken protectedToken = protector.Protect("fdp1:attacker-controlled");

        Assert.NotEqual("fdp1:attacker-controlled", protectedToken.Value, StringComparer.Ordinal);
        Assert.Equal("fdp1:attacker-controlled", protector.Unprotect(protectedToken));
    }

    private static WearableTokenProtector CreateProtector() {
        IDataProtectionProvider provider = new EphemeralDataProtectionProvider();
        return new WearableTokenProtector(provider);
    }
}
