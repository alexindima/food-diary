using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Notifications;

public sealed class Notification : AggregateRoot<NotificationId> {
    private const int TypeMaxLength = 64;
    private const int PayloadJsonMaxLength = 4000;
    private const int ReferenceIdMaxLength = 128;

    public UserId UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    private Notification() {
    }

    public static Notification Create(
        UserId userId,
        string type,
        string payloadJson,
        string? referenceId = null) {
        EnsureUserId(userId);

        if (string.IsNullOrWhiteSpace(type)) {
            throw new ArgumentException("Notification type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(payloadJson)) {
            throw new ArgumentException("Notification payload is required.", nameof(payloadJson));
        }

        var normalizedType = type.Trim();
        var normalizedPayloadJson = payloadJson.Trim();
        var normalizedReferenceId = NormalizeOptional(referenceId, ReferenceIdMaxLength, nameof(referenceId));

        var notification = new Notification {
            Id = NotificationId.New(),
            UserId = userId,
            Type = normalizedType.Length > TypeMaxLength
                ? throw new ArgumentOutOfRangeException(nameof(type), $"Type must be at most {TypeMaxLength} characters.")
                : normalizedType,
            PayloadJson = normalizedPayloadJson.Length > PayloadJsonMaxLength
                ? throw new ArgumentOutOfRangeException(nameof(payloadJson), $"PayloadJson must be at most {PayloadJsonMaxLength} characters.")
                : normalizedPayloadJson,
            ReferenceId = normalizedReferenceId,
            IsRead = false,
        };
        notification.SetCreated();
        return notification;
    }

    public void MarkAsRead() {
        if (IsRead) {
            return;
        }

        IsRead = true;
        ReadAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName) {
        if (value?.Trim() is not { Length: > 0 } normalized) {
            return null;
        }

        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }
}
