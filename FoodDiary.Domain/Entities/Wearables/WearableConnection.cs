using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Wearables;

public sealed class WearableConnection : AggregateRoot<WearableConnectionId> {
    private const int ExternalUserIdMaxLength = 256;
    private const int TokenMaxLength = 8192;

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
        DomainGuard.Defined(provider, nameof(provider));

        string normalizedExternalUserId = DomainGuard.RequiredText(externalUserId, ExternalUserIdMaxLength, nameof(externalUserId));
        string normalizedAccessToken = DomainGuard.RequiredText(accessToken, TokenMaxLength, nameof(accessToken));
        string? normalizedRefreshToken = DomainGuard.OptionalText(refreshToken, TokenMaxLength, nameof(refreshToken));
        DateTime? normalizedTokenExpiresAt = DomainGuard.OptionalUtc(tokenExpiresAtUtc, nameof(tokenExpiresAtUtc));

        var connection = new WearableConnection {
            Id = WearableConnectionId.New(),
            UserId = userId,
            Provider = provider,
            ExternalUserId = normalizedExternalUserId,
            AccessToken = normalizedAccessToken,
            RefreshToken = normalizedRefreshToken,
            TokenExpiresAtUtc = normalizedTokenExpiresAt,
            IsActive = true,
        };
        connection.SetCreated();
        return connection;
    }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTime? tokenExpiresAtUtc) {
        string normalizedAccessToken = DomainGuard.RequiredText(accessToken, TokenMaxLength, nameof(accessToken));
        string? normalizedRefreshToken = refreshToken is not null
            ? DomainGuard.OptionalText(refreshToken, TokenMaxLength, nameof(refreshToken))
            : RefreshToken;
        DateTime? normalizedTokenExpiresAt = DomainGuard.OptionalUtc(tokenExpiresAtUtc, nameof(tokenExpiresAtUtc));

        AccessToken = normalizedAccessToken;
        RefreshToken = normalizedRefreshToken;
        TokenExpiresAtUtc = normalizedTokenExpiresAt;
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
        return TokenExpiresAtUtc <= DomainTime.UtcNow;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
