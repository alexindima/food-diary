using System.Reflection;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class WearableInvariantTests {
    [Fact]
    public void WearableConnection_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableConnection.Create(
                UserId.Empty, WearableProvider.Fitbit, "ext-123", Token("token"), refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void WearableConnection_Create_WithBlankExternalUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableConnection.Create(
                UserId.New(), WearableProvider.Fitbit, "   ", Token("token"), refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void ProtectedWearableToken_FromProtectedValue_WithBlankValue_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ProtectedWearableToken.FromProtectedValue("   "));
    }

    [Fact]
    public void ProtectedWearableToken_FromProtectedValue_WithRawToken_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ProtectedWearableToken.FromProtectedValue("raw-provider-token"));
    }

    [Fact]
    public void ProtectedWearableToken_ProtectedValueFactory_IsNotPublic() {
        Assert.Null(typeof(ProtectedWearableToken).GetMethod(
            "FromProtectedValue",
            BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void WearableConnection_Create_WithValidValues_Succeeds() {
        var userId = UserId.New();
        DateTime expires = DateTime.UtcNow.AddHours(1);

        var conn = WearableConnection.Create(
            userId, WearableProvider.Garmin, "ext-456", Token("access-token"), Token("refresh-token"), expires);

        Assert.Multiple(
            () => Assert.Equal(userId, conn.UserId),
            () => Assert.Equal(WearableProvider.Garmin, conn.Provider),
            () => Assert.Equal("ext-456", conn.ExternalUserId),
            () => Assert.Equal("fdp1:access-token", conn.AccessToken.Value),
            () => Assert.Equal("fdp1:refresh-token", conn.RefreshToken?.Value),
            () => Assert.Equal(expires, conn.TokenExpiresAtUtc),
            () => Assert.True(conn.IsActive));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_WithBlankAccessToken_Throws() {
        WearableConnection conn = CreateConnection();

        Assert.Throws<ArgumentException>(() => ProtectedWearableToken.FromProtectedValue("  "));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_UpdatesValues() {
        WearableConnection conn = CreateConnection();
        DateTime newExpires = DateTime.UtcNow.AddHours(2);

        conn.UpdateTokens(Token("new-token"), Token("new-refresh"), newExpires);

        Assert.Multiple(
            () => Assert.Equal("fdp1:new-token", conn.AccessToken.Value),
            () => Assert.Equal("fdp1:new-refresh", conn.RefreshToken?.Value),
            () => Assert.Equal(newExpires, conn.TokenExpiresAtUtc));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_WithNullRefreshToken_KeepsExisting() {
        var conn = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext", Token("token"), Token("original-refresh"), tokenExpiresAtUtc: null);

        conn.UpdateTokens(Token("new-token"), refreshToken: null, tokenExpiresAtUtc: null);

        Assert.Equal("fdp1:original-refresh", conn.RefreshToken?.Value);
    }

    [Fact]
    public void WearableConnection_UpdateTokens_WhenInactive_ThrowsAndKeepsTokensCleared() {
        WearableConnection conn = CreateConnection();
        conn.Deactivate();

        Assert.Throws<InvalidOperationException>(() =>
            conn.UpdateTokens(Token("new-token"), Token("new-refresh"), DateTime.UtcNow.AddHours(1)));

        Assert.Multiple(
            () => Assert.False(conn.IsActive),
            () => Assert.True(conn.AccessToken.IsCleared),
            () => Assert.Null(conn.RefreshToken),
            () => Assert.Null(conn.TokenExpiresAtUtc));
    }

    [Fact]
    public void WearableConnection_Reconnect_ReactivatesAndReplacesIdentityAndTokens() {
        WearableConnection conn = CreateConnection();
        conn.Deactivate();
        DateTime expiresAtUtc = DateTime.UtcNow.AddHours(2);

        conn.Reconnect("new-external-user", Token("new-token"), Token("new-refresh"), expiresAtUtc);

        Assert.Multiple(
            () => Assert.True(conn.IsActive),
            () => Assert.Equal("new-external-user", conn.ExternalUserId),
            () => Assert.Equal("fdp1:new-token", conn.AccessToken.Value),
            () => Assert.Equal("fdp1:new-refresh", conn.RefreshToken?.Value),
            () => Assert.Equal(expiresAtUtc, conn.TokenExpiresAtUtc));
    }

    [Fact]
    public void WearableConnection_RecordConnectRequest_StoresFingerprint() {
        WearableConnection conn = CreateConnection();
        string requestId = new('A', 64);
        string requestHash = new('B', 64);

        conn.RecordConnectRequest(requestId, requestHash);

        Assert.Multiple(
            () => Assert.Equal(requestId, conn.LastConnectRequestId),
            () => Assert.Equal(requestHash, conn.LastConnectRequestHash),
            () => Assert.NotNull(conn.ModifiedOnUtc));
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("request", "")]
    public void WearableConnection_RecordConnectRequest_WithInvalidFingerprint_Throws(string requestId, string requestHash) {
        WearableConnection conn = CreateConnection();

        Assert.Throws<ArgumentException>(() => conn.RecordConnectRequest(requestId, requestHash));
    }

    [Fact]
    public void WearableConnection_RecordConnectRequest_WhenHashIsInvalid_DoesNotPartiallyUpdateFingerprint() {
        WearableConnection conn = CreateConnection();
        conn.RecordConnectRequest("original-request", "original-hash");
        DateTime? modifiedOnUtc = conn.ModifiedOnUtc;

        Assert.Throws<ArgumentException>(() => conn.RecordConnectRequest("new-request", "   "));

        Assert.Multiple(
            () => Assert.Equal("original-request", conn.LastConnectRequestId),
            () => Assert.Equal("original-hash", conn.LastConnectRequestHash),
            () => Assert.Equal(modifiedOnUtc, conn.ModifiedOnUtc));
    }

    [Fact]
    public void WearableConnection_MarkSynced_SetsLastSyncedAtUtc() {
        WearableConnection conn = CreateConnection();

        conn.MarkSynced();

        Assert.NotNull(conn.LastSyncedAtUtc);
    }

    [Fact]
    public void WearableConnection_Deactivate_ClearsTokensAndSetsInactive() {
        WearableConnection conn = CreateConnection();

        conn.Deactivate();

        Assert.Multiple(
            () => Assert.False(conn.IsActive),
            () => Assert.True(conn.AccessToken.IsCleared),
            () => Assert.Null(conn.RefreshToken),
            () => Assert.Null(conn.TokenExpiresAtUtc));
    }

    [Fact]
    public void WearableConnection_Deactivate_WhenAlreadyInactive_IsIdempotent() {
        WearableConnection conn = CreateConnection();
        conn.Deactivate();
        DateTime? firstModified = conn.ModifiedOnUtc;

        conn.Deactivate();

        Assert.Equal(firstModified, conn.ModifiedOnUtc);
    }

    [Fact]
    public void WearableConnection_IsTokenExpired_WhenExpired_ReturnsTrue() {
        var conn = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext", Token("token"), refreshToken: null,
            DateTime.UtcNow.AddMinutes(-1));

        Assert.True(conn.IsTokenExpired());
    }

    [Fact]
    public void WearableConnection_IsTokenExpired_WhenNoExpiry_ReturnsFalse() {
        var conn = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext", Token("token"), refreshToken: null, tokenExpiresAtUtc: null);

        Assert.False(conn.IsTokenExpired());
    }

    [Fact]
    public void WearableSyncEntry_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableSyncEntry.Create(
                UserId.Empty, WearableProvider.Fitbit, WearableDataType.Steps,
                DateTime.UtcNow, 10000));
    }

    [Fact]
    public void WearableSyncEntry_Create_StoresDateOnly() {
        var dateTime = new DateTime(2026, 3, 15, 14, 30, 45);
        var entry = WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, WearableDataType.Steps,
            dateTime, 10000);

        Assert.Equal(dateTime.Date, entry.Date);
    }

    [Theory]
    [InlineData(WearableDataType.Steps)]
    [InlineData(WearableDataType.HeartRate)]
    [InlineData(WearableDataType.CaloriesBurned)]
    [InlineData(WearableDataType.ActiveMinutes)]
    [InlineData(WearableDataType.SleepMinutes)]
    public void WearableSyncEntry_Create_WithNegativeValue_Throws(WearableDataType dataType) {
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, dataType, DateTime.UtcNow, value: -1));
    }

    [Fact]
    public void WearableSyncEntry_UpdateValue_WithNegativeValue_ThrowsWithoutChangingValue() {
        var entry = WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, WearableDataType.Steps,
            DateTime.UtcNow, 10000);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateValue(value: -1));

        Assert.Multiple(
            () => Assert.Equal(10000, entry.Value),
            () => Assert.Null(entry.ModifiedOnUtc));
    }

    [Fact]
    public void WearableSyncEntry_UpdateValue_WithDifferentValue_SetsModifiedOnUtc() {
        var entry = WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, WearableDataType.Steps,
            DateTime.UtcNow, 10000);

        entry.UpdateValue(12000);

        Assert.Equal(12000, entry.Value);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void WearableSyncEntry_UpdateValue_WithSameValue_DoesNotSetModifiedOnUtc() {
        var entry = WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, WearableDataType.Steps,
            DateTime.UtcNow, 10000);

        entry.UpdateValue(10000);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void WearableSyncEntry_UpdateValue_WithNearlyEqualValue_DoesNotSetModifiedOnUtc() {
        var entry = WearableSyncEntry.Create(
            UserId.New(), WearableProvider.Fitbit, WearableDataType.Steps,
            DateTime.UtcNow, 10000);

        entry.UpdateValue(10000.0000001);

        Assert.Null(entry.ModifiedOnUtc);
    }

    private static WearableConnection CreateConnection() {
        return WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext-123", Token("access-token"), Token("refresh-token"),
            DateTime.UtcNow.AddHours(1));
    }

    private static ProtectedWearableToken Token(string value) =>
        ProtectedWearableToken.FromProtectedValue($"fdp1:{value}");
}
