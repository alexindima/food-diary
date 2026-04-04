using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Notifications;

public sealed class Notification : AggregateRoot<NotificationId> {
    private const int TypeMaxLength = 64;
    private const int TitleMaxLength = 256;
    private const int BodyMaxLength = 1000;

    public UserId UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public string? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    private Notification() {
    }

    public static Notification Create(
        UserId userId,
        string type,
        string title,
        string? body = null,
        string? referenceId = null) {
        EnsureUserId(userId);

        if (string.IsNullOrWhiteSpace(type)) {
            throw new ArgumentException("Notification type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(title)) {
            throw new ArgumentException("Notification title is required.", nameof(title));
        }

        var notification = new Notification {
            Id = NotificationId.New(),
            UserId = userId,
            Type = type.Trim().Length > TypeMaxLength
                ? throw new ArgumentOutOfRangeException(nameof(type), $"Type must be at most {TypeMaxLength} characters.")
                : type.Trim(),
            Title = title.Trim().Length > TitleMaxLength
                ? throw new ArgumentOutOfRangeException(nameof(title), $"Title must be at most {TitleMaxLength} characters.")
                : title.Trim(),
            Body = body?.Trim() is { Length: > 0 } trimmedBody
                ? trimmedBody.Length > BodyMaxLength
                    ? throw new ArgumentOutOfRangeException(nameof(body), $"Body must be at most {BodyMaxLength} characters.")
                    : trimmedBody
                : null,
            ReferenceId = referenceId,
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
}
