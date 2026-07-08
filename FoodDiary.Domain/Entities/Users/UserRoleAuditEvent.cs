using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed class UserRoleAuditEvent : Entity<Guid> {
    private const int RoleNameMaxLength = 64;
    private const int SourceMaxLength = 64;

    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public UserRoleAuditAction Action { get; private set; }
    public UserId? ActorUserId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private UserRoleAuditEvent() {
    }

    public static UserRoleAuditEvent Create(
        UserId userId,
        Role role,
        UserRoleAuditAction action,
        UserId? actorUserId,
        string source,
        DateTime occurredAtUtc) {
        ArgumentNullException.ThrowIfNull(role);

        if (userId == UserId.Empty) {
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        }

        if (actorUserId == UserId.Empty) {
            throw new ArgumentException("Actor user id must not be empty.", nameof(actorUserId));
        }

        var auditEvent = new UserRoleAuditEvent {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            RoleName = NormalizeRequiredText(role.Name, nameof(role), RoleNameMaxLength),
            Action = action,
            ActorUserId = actorUserId,
            Source = NormalizeRequiredText(source, nameof(source), SourceMaxLength),
            OccurredAtUtc = NormalizeUtcTimestamp(occurredAtUtc, nameof(occurredAtUtc)),
        };
        auditEvent.SetCreated(auditEvent.OccurredAtUtc);
        return auditEvent;
    }

    private static string NormalizeRequiredText(string value, string paramName, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtcTimestamp(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }
}
