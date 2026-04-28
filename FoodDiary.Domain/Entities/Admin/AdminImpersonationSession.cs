using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Admin;

public sealed class AdminImpersonationSession : Entity<Guid> {
    public UserId ActorUserId { get; private set; }
    public UserId TargetUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? ActorIpAddress { get; private set; }
    public string? ActorUserAgent { get; private set; }
    public DateTime StartedAtUtc { get; private set; }

    private AdminImpersonationSession() {
    }

    public static AdminImpersonationSession Start(
        UserId actorUserId,
        UserId targetUserId,
        string reason,
        string? actorIpAddress,
        string? actorUserAgent,
        DateTime startedAtUtc) {
        if (actorUserId.Value == Guid.Empty) {
            throw new ArgumentException("Actor user id must not be empty.", nameof(actorUserId));
        }

        if (targetUserId.Value == Guid.Empty) {
            throw new ArgumentException("Target user id must not be empty.", nameof(targetUserId));
        }

        var normalizedReason = NormalizeReason(reason);

        var session = new AdminImpersonationSession {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Reason = normalizedReason,
            ActorIpAddress = NormalizeOptionalText(actorIpAddress, maxLength: 128),
            ActorUserAgent = NormalizeOptionalText(actorUserAgent, maxLength: 512),
            StartedAtUtc = NormalizeUtcTimestamp(startedAtUtc, nameof(startedAtUtc))
        };
        session.SetCreated(session.StartedAtUtc);
        return session;
    }

    private static string NormalizeReason(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Reason is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length is < 10 or > 500) {
            throw new ArgumentOutOfRangeException(nameof(value), "Reason must be between 10 and 500 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtcTimestamp(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }
}
