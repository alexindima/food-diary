using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Wearables;

public sealed class WearableConnection : AggregateRoot<WearableConnectionId> {
    private const int ExternalUserIdMaxLength = 256;
    private const int IdempotencyValueMaxLength = 64;

    public UserId UserId { get; private set; }
    public WearableProvider Provider { get; private set; }
    public string ExternalUserId { get; private set; } = string.Empty;
    public ProtectedWearableToken AccessToken { get; private set; }
    public ProtectedWearableToken? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAtUtc { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public string? LastConnectRequestId { get; private set; }
    public string? LastConnectRequestHash { get; private set; }

    public User User { get; private set; } = null!;

    private WearableConnection() {
    }

    public static WearableConnection Create(
        UserId userId,
        WearableProvider provider,
        string externalUserId,
        ProtectedWearableToken accessToken,
        ProtectedWearableToken? refreshToken,
        DateTime? tokenExpiresAtUtc) {
        EnsureUserId(userId);
        DomainGuard.Defined(provider, nameof(provider));

        string normalizedExternalUserId = DomainGuard.RequiredText(externalUserId, ExternalUserIdMaxLength, nameof(externalUserId));
        EnsureProtected(accessToken, nameof(accessToken));
        EnsureProtected(refreshToken, nameof(refreshToken));
        DateTime? normalizedTokenExpiresAt = DomainGuard.OptionalUtc(tokenExpiresAtUtc, nameof(tokenExpiresAtUtc));

        var connection = new WearableConnection {
            Id = WearableConnectionId.New(),
            UserId = userId,
            Provider = provider,
            ExternalUserId = normalizedExternalUserId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAtUtc = normalizedTokenExpiresAt,
            IsActive = true,
        };
        connection.SetCreated();
        return connection;
    }

    public void UpdateTokens(ProtectedWearableToken accessToken, ProtectedWearableToken? refreshToken, DateTime? tokenExpiresAtUtc) {
        if (!IsActive) {
            throw new InvalidOperationException("Tokens cannot be updated for an inactive wearable connection.");
        }

        EnsureProtected(accessToken, nameof(accessToken));
        EnsureProtected(refreshToken, nameof(refreshToken));
        ProtectedWearableToken? normalizedRefreshToken = refreshToken ?? RefreshToken;
        DateTime? normalizedTokenExpiresAt = DomainGuard.OptionalUtc(tokenExpiresAtUtc, nameof(tokenExpiresAtUtc));

        AccessToken = accessToken;
        RefreshToken = normalizedRefreshToken;
        TokenExpiresAtUtc = normalizedTokenExpiresAt;
        SetModified();
    }

    public void Reconnect(
        string externalUserId,
        ProtectedWearableToken accessToken,
        ProtectedWearableToken? refreshToken,
        DateTime? tokenExpiresAtUtc) {
        string normalizedExternalUserId = DomainGuard.RequiredText(externalUserId, ExternalUserIdMaxLength, nameof(externalUserId));
        EnsureProtected(accessToken, nameof(accessToken));
        EnsureProtected(refreshToken, nameof(refreshToken));
        DateTime? normalizedTokenExpiresAt = DomainGuard.OptionalUtc(tokenExpiresAtUtc, nameof(tokenExpiresAtUtc));

        ExternalUserId = normalizedExternalUserId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAtUtc = normalizedTokenExpiresAt;
        IsActive = true;
        SetModified();
    }

    public void RecordConnectRequest(string requestId, string requestHash) {
        string normalizedRequestId = DomainGuard.RequiredText(requestId, IdempotencyValueMaxLength, nameof(requestId));
        string normalizedRequestHash = DomainGuard.RequiredText(requestHash, IdempotencyValueMaxLength, nameof(requestHash));

        LastConnectRequestId = normalizedRequestId;
        LastConnectRequestHash = normalizedRequestHash;
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
        AccessToken = ProtectedWearableToken.Cleared;
        RefreshToken = null;
        TokenExpiresAtUtc = null;
        SetModified();
    }

    public bool IsTokenExpired() {
        return TokenExpiresAtUtc <= DomainTime.UtcNow;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureProtected(ProtectedWearableToken token, string paramName) {
        if (string.IsNullOrEmpty(token.Value) || !token.IsProtected) {
            throw new ArgumentException("Wearable token must be protected before persistence.", paramName);
        }
    }

    private static void EnsureProtected(ProtectedWearableToken? token, string paramName) {
        if (token.HasValue) {
            EnsureProtected(token.Value, paramName);
        }
    }
}
