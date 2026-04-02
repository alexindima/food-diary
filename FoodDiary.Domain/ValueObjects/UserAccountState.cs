namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAccountState(
    long? TelegramUserId,
    long AiInputTokenLimit,
    long AiOutputTokenLimit,
    bool IsActive,
    DateTime? DeletedAt) {
    public static UserAccountState CreateInitial(long defaultAiInputTokenLimit, long defaultAiOutputTokenLimit) {
        if (defaultAiInputTokenLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(defaultAiInputTokenLimit), "Input limit must be non-negative.");
        }

        if (defaultAiOutputTokenLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(defaultAiOutputTokenLimit), "Output limit must be non-negative.");
        }

        return new UserAccountState(
            TelegramUserId: null,
            AiInputTokenLimit: defaultAiInputTokenLimit,
            AiOutputTokenLimit: defaultAiOutputTokenLimit,
            IsActive: true,
            DeletedAt: null);
    }

    public UserAccountState WithTelegram(long? telegramUserId) {
        return this with { TelegramUserId = telegramUserId };
    }

    public UserAccountState WithAiTokenLimits(long? inputLimit, long? outputLimit) {
        if (inputLimit.HasValue && inputLimit.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(inputLimit), "Input limit must be non-negative.");
        }

        if (outputLimit.HasValue && outputLimit.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(outputLimit), "Output limit must be non-negative.");
        }

        return this with {
            AiInputTokenLimit = inputLimit ?? AiInputTokenLimit,
            AiOutputTokenLimit = outputLimit ?? AiOutputTokenLimit
        };
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
