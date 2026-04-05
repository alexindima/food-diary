using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Wearables;

public sealed class WearableConnection : AggregateRoot<WearableConnectionId> {
    public UserId UserId { get; private set; }
    public WearableProvider Provider { get; private set; }
    public string ExternalUserId { get; private set; } = string.Empty;
    public string AccessToken { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAtUtc { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public User User { get; private set; } = null!;

    private WearableConnection() {
    }

    public static WearableConnection Create(
        UserId userId,
        WearableProvider provider,
        string externalUserId,
        string accessToken,
        string? refreshToken,
        DateTime? tokenExpiresAtUtc) {
        EnsureUserId(userId);

        if (string.IsNullOrWhiteSpace(externalUserId)) {
            throw new ArgumentException("External user ID is required.", nameof(externalUserId));
        }

        if (string.IsNullOrWhiteSpace(accessToken)) {
            throw new ArgumentException("Access token is required.", nameof(accessToken));
        }

        var connection = new WearableConnection {
            Id = WearableConnectionId.New(),
            UserId = userId,
            Provider = provider,
            ExternalUserId = externalUserId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAtUtc = tokenExpiresAtUtc,
            IsActive = true,
        };
        connection.SetCreated();
        return connection;
    }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTime? tokenExpiresAtUtc) {
        if (string.IsNullOrWhiteSpace(accessToken)) {
            throw new ArgumentException("Access token is required.", nameof(accessToken));
        }

        AccessToken = accessToken;
        RefreshToken = refreshToken ?? RefreshToken;
        TokenExpiresAtUtc = tokenExpiresAtUtc;
        SetModified();
    }

    public void MarkSynced() {
        LastSyncedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void Deactivate() {
        if (!IsActive) {
            return;
        }

        IsActive = false;
        AccessToken = string.Empty;
        RefreshToken = null;
        TokenExpiresAtUtc = null;
        SetModified();
    }

    public bool IsTokenExpired() {
        return TokenExpiresAtUtc.HasValue && TokenExpiresAtUtc.Value <= DomainTime.UtcNow;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
