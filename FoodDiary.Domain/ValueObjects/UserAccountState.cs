namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAccountState(
    long? TelegramUserId,
    bool IsActive,
    DateTime? DeletedAt) {
    public static UserAccountState CreateInitial() {
        return new UserAccountState(
            TelegramUserId: null,
            IsActive: true,
            DeletedAt: null);
    }

    public UserAccountState WithTelegram(long? telegramUserId) {
        return this with { TelegramUserId = telegramUserId };
    }

    public UserAccountState Deactivate() {
        return this with { IsActive = false };
    }

    public UserAccountState Activate() {
        return this with { IsActive = true };
    }

    public UserAccountState MarkDeleted(DateTime deletedAtUtc) {
        return this with {
            DeletedAt = deletedAtUtc,
            IsActive = false
        };
    }

    public UserAccountState Restore() {
        return this with {
            DeletedAt = null,
            IsActive = true
        };
    }
}
