using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Wearables.Domain.ValueObjects;

namespace FoodDiary.Modules.Wearables.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearableConnectionStorageTests {
    private static ProtectedWearableToken ProtectedToken(string value) =>
        ProtectedWearableToken.FromProtectedValue($"fdp1:{value}");

    [Fact]
    public void StorageBoundValues_AreValidatedAndUtcNormalized() {
        var localExpiry = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local);
        var wearable = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, " external ", ProtectedToken("access"), ProtectedToken("refresh"), localExpiry);
        Assert.Multiple(
            () => Assert.Equal("external", wearable.ExternalUserId),
            () => Assert.Equal("fdp1:access", wearable.AccessToken.Value),
            () => Assert.Equal(DateTimeKind.Utc, wearable.TokenExpiresAtUtc!.Value.Kind));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: new string('x', 257),
            accessToken: ProtectedToken("access"),
            refreshToken: null,
            tokenExpiresAtUtc: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: "external",
            accessToken: ProtectedWearableToken.FromProtectedValue("fdp1:" + new string('x', 8188)),
            refreshToken: null,
            tokenExpiresAtUtc: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: "external",
            accessToken: ProtectedToken("access"),
            refreshToken: null,
            tokenExpiresAtUtc: new DateTime(year: 2026, month: 1, day: 1)));
    }
}
