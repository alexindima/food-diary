using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed class UserActivity {
    public UserId UserId { get; private set; }
    public DateTime LastActiveAtUtc { get; private set; }

    private UserActivity() { }

    public static UserActivity Create(UserId userId, DateTime occurredAtUtc) {
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("Activity requires a user.", nameof(userId));
        }
        if (occurredAtUtc.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("Activity requires a UTC timestamp.", nameof(occurredAtUtc));
        }
        return new UserActivity { UserId = userId, LastActiveAtUtc = occurredAtUtc };
    }
}
