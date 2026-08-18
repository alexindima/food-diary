using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class WearableOAuthStateServiceTests {
    [Fact]
    public void IsValidState_WithMatchingUserAndProvider_ReturnsTrue() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();

        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.True(service.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void CreateState_DoesNotExposeProtectedPayload() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();

        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.Multiple(
            () => Assert.DoesNotContain(userId.Value.ToString("D"), state, StringComparison.OrdinalIgnoreCase),
            () => Assert.DoesNotContain("client-state", state, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Fitbit", state, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsValidState_WhenStateIsTampered_ReturnsFalse() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();
        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState($"{state}x", userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenUserDiffers_ReturnsFalse() {
        WearableOAuthStateService service = CreateService();
        string state = service.CreateState(UserId.New(), WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState(state, UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenProviderDiffers_ReturnsFalse() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();
        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState(state, userId, WearableProvider.Garmin));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-protected-state")]
    public void IsValidState_WhenStateShapeInvalid_ReturnsFalse(string state) {
        WearableOAuthStateService service = CreateService();

        Assert.False(service.IsValidState(state, UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenDataProtectionKeyDiffers_ReturnsFalse() {
        var userId = UserId.New();
        WearableOAuthStateService issuer = CreateService();
        string state = issuer.CreateState(userId, WearableProvider.Fitbit, "client-state");
        WearableOAuthStateService validator = CreateService();

        Assert.False(validator.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenStateExpired_ReturnsFalse() {
        var userId = UserId.New();
        var provider = new EphemeralDataProtectionProvider();
        WearableOAuthStateService issuer = CreateService(
            new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            provider);
        string state = issuer.CreateState(userId, WearableProvider.Fitbit, "client-state");
        WearableOAuthStateService validator = CreateService(
            new DateTime(2026, 5, 31, 0, 11, 0, DateTimeKind.Utc),
            provider);

        Assert.False(validator.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    private static WearableOAuthStateService CreateService(
        DateTime? utcNow = null,
        IDataProtectionProvider? provider = null) =>
        new(
            provider ?? new EphemeralDataProtectionProvider(),
            new StubTimeProvider(utcNow ?? new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)));

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
