using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class WearableInvariantTests {
    [Fact]
    public void WearableConnection_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableConnection.Create(
                UserId.Empty, WearableProvider.Fitbit, "ext-123", "token", refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void WearableConnection_Create_WithBlankExternalUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableConnection.Create(
                UserId.New(), WearableProvider.Fitbit, "   ", "token", refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void WearableConnection_Create_WithBlankAccessToken_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WearableConnection.Create(
                UserId.New(), WearableProvider.Fitbit, "ext-123", "   ", refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void WearableConnection_Create_WithValidValues_Succeeds() {
        var userId = UserId.New();
        DateTime expires = DateTime.UtcNow.AddHours(1);

        var conn = WearableConnection.Create(
            userId, WearableProvider.GoogleFit, "ext-456", "access-token", "refresh-token", expires);

        Assert.Multiple(
            () => Assert.Equal(userId, conn.UserId),
            () => Assert.Equal(WearableProvider.GoogleFit, conn.Provider),
            () => Assert.Equal("ext-456", conn.ExternalUserId),
            () => Assert.Equal("access-token", conn.AccessToken),
            () => Assert.Equal("refresh-token", conn.RefreshToken),
            () => Assert.Equal(expires, conn.TokenExpiresAtUtc),
            () => Assert.True(conn.IsActive));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_WithBlankAccessToken_Throws() {
        WearableConnection conn = CreateConnection();

        Assert.Throws<ArgumentException>(() => conn.UpdateTokens("  ", refreshToken: null, tokenExpiresAtUtc: null));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_UpdatesValues() {
        WearableConnection conn = CreateConnection();
        DateTime newExpires = DateTime.UtcNow.AddHours(2);

        conn.UpdateTokens("new-token", "new-refresh", newExpires);

        Assert.Multiple(
            () => Assert.Equal("new-token", conn.AccessToken),
            () => Assert.Equal("new-refresh", conn.RefreshToken),
            () => Assert.Equal(newExpires, conn.TokenExpiresAtUtc));
    }

    [Fact]
    public void WearableConnection_UpdateTokens_WithNullRefreshToken_KeepsExisting() {
        var conn = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext", "token", "original-refresh", tokenExpiresAtUtc: null);

        conn.UpdateTokens("new-token", refreshToken: null, tokenExpiresAtUtc: null);

        Assert.Equal("original-refresh", conn.RefreshToken);
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
            () => Assert.Equal(string.Empty, conn.AccessToken),
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
            UserId.New(), WearableProvider.Fitbit, "ext", "token", refreshToken: null,
            DateTime.UtcNow.AddMinutes(-1));

        Assert.True(conn.IsTokenExpired());
    }

    [Fact]
    public void WearableConnection_IsTokenExpired_WhenNoExpiry_ReturnsFalse() {
        var conn = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, "ext", "token", refreshToken: null, tokenExpiresAtUtc: null);

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
            UserId.New(), WearableProvider.Fitbit, "ext-123", "access-token", "refresh-token",
            DateTime.UtcNow.AddHours(1));
    }
}
